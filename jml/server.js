// server.js - 简单的状态同步服务器
const WebSocket = require('ws');

const wss = new WebSocket.Server({ port: 8080 });
let globalGameState = null; // 服务器暂存最新的游戏状态

console.log("WS Server started on port 8080");

wss.on('connection', (ws) => {
  // 1. 新人加入，发送当前最新状态
  if (globalGameState) {
    ws.send(JSON.stringify({ type: 'SYNC_STATE', payload: globalGameState }));
  }

  ws.on('message', (message) => {
    try {
      const data = JSON.parse(message);
      
      // 2. 收到更新指令，广播给所有人
      if (data.type === 'UPDATE_STATE') {
        globalGameState = data.payload; // 更新服务器缓存
        broadcast(data); // 广播给其他客户端
      }
      
      // 3. 简单的掷骰子广播（为了防作弊，骰子结果最好广播出来）
      if (data.type === 'BROADCAST_LOG') {
        broadcast(data);
      }
      
    } catch (e) {
      console.error(e);
    }
  });
});

function broadcast(data) {
  wss.clients.forEach(client => {
    if (client.readyState === WebSocket.OPEN) {
      client.send(JSON.stringify(data));
    }
  });
}