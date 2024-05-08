using GreeControl;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;

namespace GreeControlAPI.Controllers
{

    public class GreeController : ControllerBase
    {
        public async Task<List<SubInfo>> FindSubDevice(string mac, string ips)
        {
            List<SubInfo> jgsub = new List<SubInfo>();

            using (var udp = new UdpClient())
            {
                udp.EnableBroadcast = true;

                var bytes = Encoding.ASCII.GetBytes("{ \"t\": \"subDev\",\"mac\": \"" + mac + "\", \"i\": 0,}");

                var sent = udp.SendAsync(bytes, bytes.Length, ips, 7000);

                for (int i = 0; i < 20; ++i)
                {
                    if (udp.Available > 0)
                    {
                        System.Net.IPEndPoint ipe = null;
                        var result = udp.Receive(ref ipe);
                        var Json = Encoding.UTF8.GetString(result);
                        if (Json.Contains("\"t\":\"subList\""))
                        {
                            dynamic subJsonObject = JsonConvert.DeserializeObject(Json);

                            foreach (var item in subJsonObject.list)
                            {
                                SubInfo tmp = new SubInfo((string)item.name, (string)item.mac);
                                jgsub.Add(tmp);
                            }
                        }
                        udp.Close();
                        udp.Dispose();
                        //{"t":"subList","c":4,"i":0,"list":[{"mac":"98048d18000000","name":"书房","mid":"4160","model":"","lock":0},{"mac":"f7358d18000000","name":"次卧","mid":"4160","model":"","lock":0},{"mac":"374ca318000000","name":"主卧","mid":"4160","model":"","lock":0},{"mac":"98fcc518000000","name":"客厅","mid":"4160","model":"","lock":0}],"r":200}
                        break;
                    }
                    await Task.Delay(100);
                }
            }
            return jgsub;


        }
        [HttpGet("Scan")]
        public async Task<ActionResult<ResponseDevInfo>> Scan()
        {
            var deviceInfoResponse = new ResponseDevInfo();

            System.Net.IPEndPoint endPoint = null;

            #region 扫描主设备
            string jsonResponse = "";
            using (var udpClient = new UdpClient())
            {
                udpClient.EnableBroadcast = true;

                var bytes = Encoding.ASCII.GetBytes("{ \"t\": \"scan\" }");

                await udpClient.SendAsync(bytes, bytes.Length, "192.168.0.255", 7000);

                for (int i = 0; i < 20; ++i)
                {
                    if (udpClient.Available > 0)
                    {
                        var result = udpClient.Receive(ref endPoint);
                        var json = Encoding.ASCII.GetString(result);
                        jsonResponse = json;
                        udpClient.Close();
                        udpClient.Dispose();
                        break;
             
                    }
                    await Task.Delay(100); // Delay to wait
                }
            }
            #endregion
    
            if (jsonResponse.Contains("\"t\":\"pack\""))
            {
            
                // Scan successful
                deviceInfoResponse.Devip = endPoint.Address.ToString();
                dynamic jsonObject = JsonConvert.DeserializeObject(jsonResponse);
                string packValue = jsonObject.pack;
                var decryptedPack = Crypto.DecryptGenericData(packValue);
                //{"t":"dev","cid":"","bc":"","brand":"","catalog":"","mac":"b4430da972f5","mid":"60","model":"","name":"格力空调","lock":0,"series":"","vender":"","ver":"","subCnt":4}
                dynamic macJsonObject = JsonConvert.DeserializeObject(decryptedPack);
                var macAddress = macJsonObject.mac;
                deviceInfoResponse.Devmac = macAddress;

                jsonResponse = "";
                #region 绑定设备
                var bindRequestPack = new BindRequestPack() { MAC = deviceInfoResponse.Devmac };
                var bindRequest = DevRequest.Create(deviceInfoResponse.Devmac, Crypto.EncryptGenericData(JsonConvert.SerializeObject(bindRequestPack)), 1);
                var bindRequestJson = JsonConvert.SerializeObject(bindRequest);

                var bindRequestData = Encoding.ASCII.GetBytes(bindRequestJson);

                using (var udpClient = new UdpClient())
                {

                    var sent = udpClient.SendAsync(bindRequestData, bindRequestData.Length, endPoint.Address.ToString(), 7000);
                    for (int i = 0; i < 50; ++i)
                    {
                        if (udpClient.Available > 0)
                        {

                            var result = udpClient.Receive(ref endPoint);
                            var responseJson = Encoding.ASCII.GetString(result);
                            // Return value {"t":"pack","i":1,"uid":0,"cid":"b4430da972f5","tcid":"app","pack":"FtZAY6UHKBwtaI55He0IndbigvnBprnXzJ0aHiL7qKoYaQHXOjVSWB1HnUK/04kVUK7SvotrVLTzHkkg0Ko76Qp6SjSG+Rqyqwg5UqTE9Es="}
                            dynamic responseJsonObject = JsonConvert.DeserializeObject(responseJson);
                            string responsePackValue = responseJsonObject.pack;
                            var decryptedResponsePack = Crypto.DecryptGenericData(responsePackValue);

                            if (decryptedResponsePack.Contains("\"t\":\"bindOk\""))
                            {
                                // Binding successful
                                dynamic macJsonObject3 = JsonConvert.DeserializeObject(decryptedResponsePack);
                                var key = macJsonObject3.key;
                                deviceInfoResponse.PrivateKey = key;

                                #region 扫描子设备
                                udpClient.Close();
                                udpClient.Dispose();
                                deviceInfoResponse.Devsubinfolist = await FindSubDevice(deviceInfoResponse.Devmac, deviceInfoResponse.Devip);
                                //{"t":"subList","c":4,"i":0,"list":[{"mac":"98048d18000000","name":"书房","mid":"4160","model":"","lock":0},{"mac":"f7358d18000000","name":"次卧","mid":"4160","model":"","lock":0},{"mac":"374ca318000000","name":"主卧","mid":"4160","model":"","lock":0},{"mac":"98fcc518000000","name":"客厅","mid":"4160","model":"","lock":0}],"r":200}

                                #endregion
                          
                                break;
                            }
                        
                        }
                        await Task.Delay(100); // Delay to wait
                    }
               
                }
                #endregion
            }
            return deviceInfoResponse;
            return BadRequest("Invalid Request");
        }




        [HttpGet("GetStatus")]
        public async Task<ActionResult<DeviceStatus>> GetStatus(string mainmac, string mac, string key, string ip)
        {
            DeviceStatus DeviceStatusInfo = new DeviceStatus();

            try
            {
                DeviceStatusRequestPack drp = DeviceStatusRequestPack.Create(mac, new List<string> { "Pow", "SetTem", "WdSpd" });
                var wjiamijson = JsonConvert.SerializeObject(drp);
                var jiamipack = Crypto.EncryptData(wjiamijson, key);
                DevRequest apprqt = DevRequest.Create(mainmac, jiamipack, 0);

                var sendjson = JsonConvert.SerializeObject(apprqt);

                using (var udp = new UdpClient())
                {
                    udp.EnableBroadcast = true;

                    var bytes = Encoding.ASCII.GetBytes(sendjson);

                    var sent = udp.SendAsync(bytes, bytes.Length, ip, 7000);

                    for (int i = 0; i < 20; ++i)
                    {
                        if (udp.Available > 0)
                        {
                            System.Net.IPEndPoint endPoint = null;
                            var result = udp.Receive(ref endPoint);
                            var Json = Encoding.UTF8.GetString(result);
                            //{"t":"pack","i":0,"uid":0,"cid":"b4430da972f5","tcid":"app","pack":"RQAg80NNQ7j/V/P271B51aBqebV+x+MJNuNbH73paJE3NhAU159JOGZzyTBMXe9Q5ukMDg5kQO85RGlFehw/089TO+l/JCx7qzb38UpwylE="}

                            dynamic jsonObject = JsonConvert.DeserializeObject(Json);
                            string packValue = jsonObject.pack;
                            var decryptedPack = Crypto.DecryptData(packValue, key);
                            if (decryptedPack.Contains("\"t\":\"dat\""))
                            {
                                dynamic macJsonObject3 = JsonConvert.DeserializeObject(decryptedPack);
                                //{"t":"dat","r":200,"uid":0,"cid":"app","mac":"f7358d18000000","cols":["Pow","SetTem","WdSpd"],"dat":[0,19,5]}
                                DeviceStatusInfo.Pow = (int)macJsonObject3.dat[0];
                                DeviceStatusInfo.SetTem = (int)macJsonObject3.dat[1];
                                DeviceStatusInfo.WdSpd = (int)macJsonObject3.dat[2];
                                udp.Close();
                                udp.Dispose();
                            }

                            break;
                        }
                        await Task.Delay(100);
                    }

                }
            }
            catch (Exception ex)
            {
                // 处理异常情况
                // 可以记录日志或者返回适当的错误响应
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "An error occurred while processing your request.");
            }

            return DeviceStatusInfo;
        }

        [HttpGet("SendCMD")]
        public async Task<ActionResult<string>> SendCMD(string mainmac, string mac, string key, string ip,string CMDstr,int CMDvalue)
        {
            DeviceStatus DeviceStatusInfo = new DeviceStatus();
            CommandRequestPack crp = CommandRequestPack.Create(mac, new List<string> { CMDstr}, new List<int> { CMDvalue });
            
            
            var wjiamijson = JsonConvert.SerializeObject(crp);
            var jiamipack = Crypto.EncryptData(wjiamijson, key);
            DevRequest apprqt = DevRequest.Create(mainmac, jiamipack, 0);

            var sendjson = JsonConvert.SerializeObject(apprqt);


            using (var udp = new UdpClient())
            {
                udp.EnableBroadcast = true;

                var bytes = Encoding.ASCII.GetBytes(sendjson);

                var sent = udp.SendAsync(bytes, bytes.Length, ip, 7000);

                for (int i = 0; i < 20; ++i)
                {
                    if (udp.Available > 0)
                    {
                        System.Net.IPEndPoint endPoint = null;
                        var result = udp.Receive(ref endPoint);
                        var Json = Encoding.UTF8.GetString(result);
                        //{"t":"pack","i":0,"uid":0,"cid":"b4430da972f5","tcid":"app","pack":"RQAg80NNQ7j/V/P271B51aBqebV+x+MJNuNbH73paJE3NhAU159JOGZzyTBMXe9Q5ukMDg5kQO85RGlFehw/089TO+l/JCx7qzb38UpwylE="}

                        dynamic jsonObject = JsonConvert.DeserializeObject(Json);
                        string packValue = jsonObject.pack;
                        var decryptedPack = Crypto.DecryptData(packValue, key);
                        if (decryptedPack.Contains("\"t\":\"res\""))//t":"res",
                        {
                            udp.Close();
                            udp.Dispose();
                            return "OK";
                        }
                        break;
                    }
                    await Task.Delay(100);
                }
             
            }
            return "Error";
        }
    }
}
