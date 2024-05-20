const electron = require('electron');
const net = require('net');

let client = null;
let infiniteHealthInterval = null;

const socketOptions = {
  host: '127.0.0.1',
  port: 3000
};

function connectToServer() {
  client = net.createConnection(socketOptions, () => {
    console.log('Connected to server');
  });

  client.on('data', (data) => {
    console.log(`Received data from server: ${data}`);
  });

  client.on('error', (err) => {
    console.error('Socket error:', err);
    reconnectToServer();
  });

  client.on('end', () => {
    reconnectToServer();
  });
}

function reconnectToServer() {
  if (client) {
    client.destroy();
    client = null;
  }
  setTimeout(connectToServer, 1000); // Reconnect after 1 second
}

function sendHealthRequest(health) {
  if (client && client.writable) {
    const data = `setHealth:${health}`;
    client.write(data);
  } else {
    console.error('Client is not connected');
  }
}

document.addEventListener('DOMContentLoaded', () => {
  const infinityHealthCheckbox = document.getElementById('infinityHealthCheckbox');
  const healthInput = document.getElementById('healthInput');
  const setHealthButton = document.getElementById('setHealthButton');

  setHealthButton.addEventListener('click', () => {
    const health = parseInt(healthInput.value, 10);
    if (!isNaN(health)) {
      sendHealthRequest(health);
    } else {
      healthInput.value = 100;
    }
  });

  infinityHealthCheckbox.addEventListener('change', (event) => {
    if (event.target.checked) {
      infiniteHealthInterval = setInterval(() => {
        sendHealthRequest(9999);
      }, 500); // 0.1초마다 요청
    } else {
      clearInterval(infiniteHealthInterval);
      infiniteHealthInterval = null;
    }
  });

  connectToServer();
});

electron.ipcRenderer.on('focus-change', (e, state) => {
  document.getElementById('text1').textContent = state ? ' (overlay is clickable) ' : 'clicks go through overlay';
});

electron.ipcRenderer.on('visibility-change', (e, state) => {
  document.body.style.display = state ? 'block' : 'none';
});
