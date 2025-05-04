// 定时任务管理
class TimerManager {
    constructor() {
        this.timers = [];
        this.initEventListeners();
        this.loadTimers();
    }

    // 初始化事件监听
    initEventListeners() {
        document.getElementById('add-on-timer').addEventListener('click', () => this.showTimerDialog('on'));
        document.getElementById('add-off-timer').addEventListener('click', () => this.showTimerDialog('off'));
        
    
    }
    
 
    // 显示定时任务对话框
    showTimerDialog(action) {


        var tempdev = document.querySelector('.device-name');
        if (!tempdev) {
            alert('未找到设备信息，请刷新页面后重试');
            return;
        }

        const mac = tempdev.getAttribute('data-mac');
        const time = document.getElementById('timer-time').value;
        
        if (!mac) {
            alert('当前设备缺少MAC地址，请重新选择设备');
            return;
        }
        
        if (!time) {
            alert('请选择时间');
            return;
        }

  


        this.addTimer({
            MacAddress: mac,
            Action: (action == 'on' ? 'TurnOn' : 'TurnOff'),
            Time: new Date(time).toISOString()
        });
    }

    // 添加定时任务
    async addTimer(timer) {
        try {
            const response = await fetch('/api/Timer', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(timer)
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || '添加定时任务失败');
            }
            
            const result = await response.json();
            this.timers.push(result);
            this.renderTimers();
            this.showToast('定时任务添加成功', 'success');
        } catch (error) {
            console.error('Error:', error);
            this.showToast(error.message || '添加定时任务失败', 'error');
        }
    }

    // 显示提示信息
    showToast(message, type = 'info') {
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.textContent = message;
        document.body.appendChild(toast);

        setTimeout(() => {
            toast.remove();
        }, 3000);
    }

    // 删除定时任务
    async deleteTimer(index) {
        const timer = this.timers[index];
        if (confirm('确定要删除该定时任务吗？')) {
            try {
                const response = await fetch(`/api/Timer/${timer.mac}/${timer.time}`, {
                    method: 'DELETE'
                });

                if (response.ok) {
                    this.timers.splice(index, 1);
                    this.renderTimers();
                    this.showToast('定时任务删除成功', 'success');
                } else {
                    throw new Error('删除定时任务失败');
                }
            } catch (error) {
                console.error('Error:', error);
                this.showToast('删除定时任务失败', 'error');
            }
        }
    }

    // 加载定时任务
    async loadTimers() {
        try {
            const response = await fetch('/api/Timer');
            if (response.ok) {
                this.timers = await response.json();
                this.renderTimers();
            }
        } catch (error) {
            console.error('Error loading timers:', error);
        }
    }

    // 获取设备名称
    getDeviceName(mac) {
        const device = document.querySelector(`.device-list div[data-mac="${mac}"]`);
        return device ? device.textContent : '未知设备';
    }

    // 渲染定时任务列表
    renderTimers() {
        const container = document.getElementById('timers-container');
        container.innerHTML = this.timers.map((timer, index) => `
            <div class="timer-item">
                <span>${this.getDeviceName(timer.mac)}</span>
                <span>${timer.mac}</span>
                <span>${timer.action === 'TurnOn' ? '开机' : '关机'}</span>
                <span>${this.formatDateTime(timer.time)}</span>
                <span class="timer-action" onclick="timerManager.deleteTimer(${index})">删除</span>
            </div>
        `).join('');
    }

    // 格式化日期时间
    formatDateTime(isoString) {
        const date = new Date(isoString);
        return date.toLocaleString('zh-CN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        });
    }

    // 获取当前选中设备的MAC地址
    getSelectedDeviceMAC() {
        // 这里需要根据实际设备选择逻辑实现
        return document.querySelector('.device-list .selected')?.getAttribute('data-mac');
    }

    // 验证时间格式
    validateTime(time) {
        return /^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$/.test(time);
    }
}

// 初始化定时任务管理器
const timerManager = new TimerManager();



let mainmac = '';
let key = '';
let ip = '';

function sendCmd(macStr, cmdStr, cmdValue) {
    const url = `/SendCMD?mainmac=${mainmac}&mac=${macStr}&key=${key}&ip=${ip}&CMDstr=${cmdStr}&CMDvalue=${cmdValue}`;
    fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => console.log(data))
        .catch(error => console.error('There has been a problem with your fetch operation:', error));
}

function ScanDev() {
    const cookieName = 'deviceInfo';
    const cookieValue = getCookie(cookieName);
    if (cookieValue) {
        // Cookie exists, parse and return the data
        const data = JSON.parse(cookieValue);
        mainmac = data.devmac;
        ip = data.devip;
        key = data.privateKey;
        return Promise.resolve(data);

    } else {
        // Cookie doesn't exist, make a fetch request
        const url = `/Scan`;
        return fetch(url)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                console.log(data);
                mainmac = data.devmac;
                ip = data.devip;
                key = data.privateKey;
                // Save the data to a cookie
                const cookieValue = JSON.stringify(data);
                setCookie(cookieName, cookieValue, 360); // expire in 30 days
                return data;
            })
            .catch(error => {
                console.error('There has been a problem with your fetch operation:', error);
                return []; // <-- return an empty array or a default value
            });
    }
}

// Helper functions to get and set cookies
function getCookie(name) {
    const value = document.cookie.match(`(^|;)\\s*${name}\\s*=\\s*(.*?)$`);
    return value ? value.pop() : '';
}

function setCookie(name, value, days) {
    const expires = new Date(Date.now() + days * 24 * 60 * 60 * 1000);
    document.cookie = `${name}=${value}; expires=${expires.toUTCString()}; path=/`;
}

function getAcStatus(mac) {
    const url = `/GetStatus?mainmac=${mainmac}&mac=${mac}&key=${key}&ip=${ip}`;
    return fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            return data;
        })
        .catch(error => {
            console.error('There has been a problem with your fetch operation:', error);
            return []; // <-- return an empty array or a default value
        });
}


const temperatureGauge = document.querySelector('.temperature-gauge');
const temperatureFill = document.querySelector('.temperature-gauge .fill');
const temperatureHandle = document.querySelector('.temperature-gauge .handle');
const temperatureLabel = document.querySelector('.temperature-label');
let temperature = 20; // initial temperature
let temperatureHandleHeight = 10; // handle height
let temperatureGaugeHeight = temperatureGauge.offsetHeight; // gauge height
let minTemp = 16; // minimum temperature
let maxTemp = 30; // maximum temperature
let tempRange = maxTemp - minTemp; // temperature range
let temperatureHandlePos = (temperature - minTemp) / tempRange * (temperatureGaugeHeight - temperatureHandleHeight);
temperatureFill.style.height = `${(temperature - minTemp) / tempRange * 100}%`;
temperatureHandle.style.bottom = `${temperatureHandlePos}px`;
temperatureLabel.textContent = `${temperature}°C`;
const windSlider = document.querySelector('.wind-slider');
const windFill = document.querySelector('.wind-slider .fill');
const windHandle = document.querySelector('.wind-slider .handle');
const windLabel = document.querySelector('.wind-label');
let oldwindSpeed = 0;
let windSpeed = 0; // initial wind speed
let windHandleHeight = 10; // handle height
let windSliderHeight = windSlider.offsetHeight; // gauge height
let minWind = 0; // minimum wind speed
let maxWind = 6; // maximum wind speed
let windRange = maxWind - minWind; // wind range
let windHandlePos = (windSpeed - minWind) / windRange * (windSliderHeight - windHandleHeight);
windFill.style.height = `${(windSpeed - minWind) / windRange * 100}%`;
windHandle.style.bottom = `${windHandlePos}px`;
windLabel.textContent = `风速:${windSpeed}`;
let isDraggingTemperature = false;
let isDraggingWind = false;
let ispowdown = true;
temperatureGauge.addEventListener('mousedown', (e) => {
    if (e.target.classList.contains('handle')) {
        isDraggingTemperature = true;
        const startY = e.clientY;
        const startHandlePos = temperatureHandlePos;
        document.addEventListener('mousemove', (e) => {
            if (isDraggingTemperature) {
                const clientY = e.clientY;
                const diffY = startY - clientY; // calculate the difference in y-position
                temperatureHandlePos = startHandlePos + diffY;
                // limit the handle position within the gauge
                temperatureHandlePos = Math.min(Math.max(temperatureHandlePos, 0), temperatureGaugeHeight - temperatureHandleHeight);
                temperature = minTemp + (temperatureHandlePos / (temperatureGaugeHeight - temperatureHandleHeight)) * tempRange;
                temperatureFill.style.height = `${(temperature - minTemp) / tempRange * 100}%`;
                temperatureHandle.style.bottom = `${temperatureHandlePos}px`;
                temperatureLabel.textContent = `${Math.round(temperature)}°C`;
            }
        });
    }
});
windSlider.addEventListener('mousedown', (e) => {
    if (e.target.classList.contains('handle')) {
        if (windSpeed == 0) {
            ispowdown = false;
        }
        isDraggingWind = true;
        const startY = e.clientY;
        const startHandlePos = windHandlePos;
        document.addEventListener('mousemove', (e) => {
            if (isDraggingWind) {
                const clientY = e.clientY;
                const diffY = startY - clientY; // calculate the difference in y-position
                windHandlePos = startHandlePos + diffY;
                windHandlePos = startHandlePos + diffY;
                windHandlePos = Math.min(Math.max(windHandlePos, 0), windSliderHeight - windHandleHeight);
                windSpeed = minWind + (windHandlePos / (windSliderHeight - windHandleHeight)) * windRange;
                windFill.style.height = `${(windSpeed - minWind) / windRange * 100}%`;
                windHandle.style.bottom = `${windHandlePos}px`;

                if (Math.round(windSpeed) == 3) {
                    windLabel.textContent = `自动:${Math.round(windSpeed)}`;
                }
                else {
                    if (Math.round(windSpeed) == 0) {
                        windLabel.textContent = `关机:${Math.round(windSpeed)}`;
                    } else {

                        windLabel.textContent = `风速:${Math.round(windSpeed)}`;
                    }

                }

            }
        });
    }
});
document.addEventListener('mouseup', () => {
    var tempdev = document.querySelector('.device-name');
    if (isDraggingTemperature) {
        isDraggingTemperature = false;
        sendCmd(tempdev.getAttribute('data-mac'), "SetTem", Math.round(temperature));
    }
    if (isDraggingWind) {
        isDraggingWind = false;
        if (Math.round(windSpeed) == 0) {
            sendCmd(tempdev.getAttribute('data-mac'), "Pow", 0);
        } else {
            if (!ispowdown) {
                sendCmd(tempdev.getAttribute('data-mac'), "Pow", 1);
            }
            sendCmd(tempdev.getAttribute('data-mac'), "WdSpd", Math.round(windSpeed) == "3" ? "0" : Math.round(windSpeed));
        }
    }
});

//温度手机滑动代码
let startY;
let startHandlePos;
function handleTemperatureDrag(e) {
    if (isDraggingTemperature) {
        const clientY = e.touches ? e.touches[0].clientY : e.clientY;
        const diffY = startY - clientY; // 计算y轴位置的差值
        temperatureHandlePos = startHandlePos + diffY;
        // 限制滑块位置在表尺内
        temperatureHandlePos = Math.min(Math.max(temperatureHandlePos, 0), temperatureGaugeHeight - temperatureHandleHeight);
        temperature = minTemp + (temperatureHandlePos / (temperatureGaugeHeight - temperatureHandleHeight)) * tempRange;
        temperatureFill.style.height = `${(temperature - minTemp) / tempRange * 100}%`;
        temperatureHandle.style.bottom = `${temperatureHandlePos}px`;
        temperatureLabel.textContent = `${Math.round(temperature)}°C`;
    }
}

temperatureHandle.addEventListener('touchstart', (e) => {

    e.preventDefault(); // 确保 touchend 能被触发
    isDraggingTemperature = true;
    startY = e.touches[0].clientY;
    startHandlePos = temperatureHandlePos;
    document.addEventListener('touchmove', handleTemperatureDrag);
});

document.addEventListener('touchend', event => {
    var tempdev = document.querySelector('.device-name');
    if (isDraggingTemperature) {
        sendCmd(tempdev.getAttribute('data-mac'), "SetTem", Math.round(temperature));
        isDraggingTemperature = false;
        // 使用 event.changedTouches[0] 而不是 event.touches[0]
        let touchInfo = event.changedTouches[0];

    }
    document.removeEventListener('touchmove', handleTemperatureDrag);


    if (isDraggingWind) {
        if (Math.round(windSpeed) == 0) {
            sendCmd(tempdev.getAttribute('data-mac'), "Pow", 0);
        } else {
            if (!ispowdown) {
                sendCmd(tempdev.getAttribute('data-mac'), "Pow", 1);
            }

            sendCmd(tempdev.getAttribute('data-mac'), "WdSpd", Math.round(windSpeed) == "3" ? "0" : Math.round(windSpeed));
        }
        isDraggingWind = false;
        // use event.changedTouches[0] instead of event.touches[0]
        let touchInfo = event.changedTouches[0];
    }
    document.removeEventListener('touchmove', handleWindDrag);








});
//温度手机滑动代码 结束


//风速手机滑动代码

let startY2;
let startHandlePos2;
function handleWindDrag(e) {
    if (isDraggingWind) {
        const clientY = e.touches ? e.touches[0].clientY : e.clientY;
        const diffY = startY2 - clientY; // calculate the difference in y-position
        windHandlePos = startHandlePos2 + diffY;
        windHandlePos = Math.min(Math.max(windHandlePos, 0), windSliderHeight - windHandleHeight);
        windSpeed = minWind + (windHandlePos / (windSliderHeight - windHandleHeight)) * windRange;
        windFill.style.height = `${(windSpeed - minWind) / windRange * 100}%`;
        windHandle.style.bottom = `${windHandlePos}px`;
        if (Math.round(windSpeed) == 3) {
            windLabel.textContent = `自动:${Math.round(windSpeed)}`;
        }
        else {
            if (Math.round(windSpeed) == 0) {
                windLabel.textContent = `关机:${Math.round(windSpeed)}`;
            } else {

                windLabel.textContent = `风速:${Math.round(windSpeed)}`;
            }

        }


    }
}

windHandle.addEventListener('touchstart', (e) => {
    if (windSpeed == 0) {
        ispowdown = false;
    }
    e.preventDefault(); // ensure touchend is triggered
    isDraggingWind = true;
    startY2 = e.touches[0].clientY;
    startHandlePos2 = windHandlePos;
    document.addEventListener('touchmove', handleWindDrag);
});


//风速手机滑动代码  结束

const deviceList = document.querySelector('.device-list');
let devices = ["1", "2", "3"]; // or var devices = ["1", "2", "3"];
console.log(devices);
ScanDev().then(newDevices => {
    devices = newDevices.devsubinfolist.map(item => item.name);
    let macs = newDevices.devsubinfolist.map(item => item.mac);
    console.log(devices);
    const width = 100 / devices.length;
    devices.forEach((device, index) => {
        const deviceElement = document.createElement('div');
        deviceElement.textContent = device;
        deviceElement.style.fontWeight = 'bold'; // make the text bold
        deviceElement.style.textAlign = 'center'; // center the text
        if (index % 2 == 0) {
            deviceElement.style.backgroundColor = 'green';
        } else {
            deviceElement.style.backgroundColor = 'red'; // set the background color to red
        }
        deviceElement.setAttribute("data-mac", macs[index]);
        deviceElement.style.color = 'white'; // set the text color to white (which contrasts well with red)
        deviceElement.style.width = `${width}%`;
        deviceElement.style.borderTopLeftRadius = '10px';
        deviceElement.style.borderTopRightRadius = '10px';
        deviceElement.style.borderBottom = '1px solid #ccc';
        deviceElement.style.cursor = 'pointer';
        deviceElement.style.position = 'absolute'; // make the device elements absolute
        deviceElement.style.bottom = '0'; // stick them to the bottom of the parent div
        deviceElement.style.height = '80%'; // set the initial height to 80% of the parent div
        deviceElement.style.left = `${index * width}%`; // calculate the left position based on the index

        if (index == 0) {
            deviceElement.style.height = '100%'; // set the height to 100% when selected
            const devicename = document.querySelector('.device-name');
            devicename.setAttribute('data-mac', deviceElement.getAttribute('data-mac'));
            devicename.textContent = device;
            getAcStatus(deviceElement.getAttribute('data-mac')).then(newdata => {
                if (newdata.pow == 0) {
                    setSpeed(0, "关机");//设置为关机
                } else {
                    if (newdata.wdSpd == 0) {
                        setSpeed(3, "自动");//如果风速0 则为自动风速
                    }
                    else {

                        setSpeed(newdata.wdSpd, "风速");//如果风速0 则为自动风速
                    }


                }
                setTemp(newdata.setTem);
            });
            deviceElement.classList.add('selected');
        }
        deviceElement.addEventListener('click', () => {
            const unselectedDevices = document.querySelectorAll('.device-list div');
            unselectedDevices.forEach((device) => {
                device.style.height = '80%';
            });
            deviceElement.style.height = '100%'; // set the height to 100% when selected
            const devicename = document.querySelector('.device-name');
            devicename.setAttribute('data-mac', deviceElement.getAttribute('data-mac'));
            devicename.textContent = device;
            getAcStatus(deviceElement.getAttribute('data-mac')).then(newdata => {
                if (newdata.pow == 0) {
                    setSpeed(0, "关机");//设置为关机
                } else {
                    if (newdata.wdSpd == 0) {
                        setSpeed(3, "自动");//如果风速0 则为自动风速
                    }
                    else {

                        setSpeed(newdata.wdSpd, "风速");//如果风速0 则为自动风速
                    }


                }
                setTemp(newdata.setTem);
            });
            deviceElement.classList.add('selected');
            const otherDevices = document.querySelectorAll('.device-list div.selected');
            otherDevices.forEach((device) => {
                device.classList.remove('selected');

            });
        });
        deviceList.appendChild(deviceElement);
    });
    deviceList.style.background = 'transparent';
    deviceList.style.height = '50px'; // adjust the height to fit your needs
});
function setSpeed(Speed, namestr) {
    windSpeed = Speed;
    windFill.style.height = `${(windSpeed - minWind) / windRange * 100}%`;
    windHandle.style.bottom = `${windHandlePos}px`;
    windLabel.textContent = namestr + `:${Math.round(windSpeed)}`;
    const sliderHeight = windSlider.offsetHeight; // or whatever the maximum height of the slider is
    windHandlePos = ((windSpeed - minWind) / windRange * 100) * (sliderHeight / 100);
    windHandle.style.bottom = `${windHandlePos}px`;
}
function setTemp(Tempnum) {
    temperature = Tempnum;
    temperatureFill.style.height = `${(temperature - minTemp) / tempRange * 100}%`;
    temperatureHandle.style.bottom = `${temperatureHandlePos}px`;
    temperatureLabel.textContent = `${Math.round(temperature)}°C`;
    const sliderHeight = temperatureGauge.offsetHeight; // or whatever the maximum height of the slider is
    temperatureHandlePos = ((temperature - minTemp) / tempRange * 100) * (sliderHeight / 100);
    temperatureHandle.style.bottom = `${temperatureHandlePos}px`;
}
