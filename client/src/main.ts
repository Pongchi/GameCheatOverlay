import { app, BrowserWindow, globalShortcut } from 'electron';
import { OverlayController, OVERLAY_WINDOW_OPTS } from 'electron-overlay-window';
import path from 'path';
import net from 'net';

// Handle creating/removing shortcuts on Windows when installing/uninstalling.
if (require('electron-squirrel-startup')) {
  app.quit();
}

let window: BrowserWindow

const toggleMouseKey = 'CmdOrCtrl + J';
const toggleShowKey = 'CmdOrCtrl + K';
const turnOffKey = 'CmdOrCtrl + Q'

const createWindow = () => {
  // Create the browser window.
  window = new BrowserWindow({
    width: 400,
    height: 300,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      nodeIntegration: true,
      contextIsolation: false
    },
    ...OVERLAY_WINDOW_OPTS
  });

  if (MAIN_WINDOW_VITE_DEV_SERVER_URL) {
    window.loadURL(MAIN_WINDOW_VITE_DEV_SERVER_URL);
  } else {
    window.loadFile(path.join(__dirname, `../renderer/${MAIN_WINDOW_VITE_NAME}/index.html`));
  }

  initInteractive();

  OverlayController.attachByTitle(window, 'AssaultCube');

  // mainWindow.webContents.openDevTools();
};

function initInteractive () {
  let isInteractable = false

  function toggleOverlayState () {
    if (isInteractable) {
      isInteractable = false
      OverlayController.focusTarget()
      window.webContents.send('focus-change', false)
    } else {
      isInteractable = true
      OverlayController.activateOverlay()
      window.webContents.send('focus-change', true)
    }
  }

  window.on('blur', () => {
    isInteractable = false
    window.webContents.send('focus-change', false)
  })

  globalShortcut.register(toggleMouseKey, toggleOverlayState)

  globalShortcut.register(toggleShowKey, () => {
    window.webContents.send('visibility-change', false)
  })

  globalShortcut.register(turnOffKey, () => {
    app.quit()
  })
}


app.on('ready', createWindow);
app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }z
});

app.on('activate', () => {
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow();
  }
});

// 소켓 생성
const options = {
  host: '127.0.0.1',
  port: 3000
};

const client = net.createConnection(options, () => {
  console.log('Connected to server');

  // 서버에 데이터 전송
  const data = 'setHealth:1000';
  client.write(data);
});

// 서버로부터 데이터를 받았을 때의 이벤트 핸들러
client.on('data', (data) => {
  console.log(`Received data from server: ${data}`);
});

// 서버와의 연결이 끊어졌을 때의 이벤트 핸들러
client.on('end', () => {
  console.log('Disconnected from server');
});