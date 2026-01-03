// server.js
const { Server } = require("socket.io");

const io = new Server(3000, {
  cors: { origin: "*" }
});

// 内存中暂存游戏状态 (如果有数据库更好，这里用变量模拟)
let globalGameState = {
    roomName: 'Default',
    round: 1,
    ringLevel: 0,
    currentActorId: '',
    characters: []
};

io.on("connection", (socket) => {
  console.log("Player connected:", socket.id);

  // 1. 新人进来，先把当前状态发给它
  socket.emit("sync_state", globalGameState);

  // 2. 监听客户端的状态更新
  socket.on("update_state", (newState) => {
    // 更新服务器内存
    globalGameState = newState;
    // 广播给【除自己以外】的所有人 (或者 io.emit 给所有人也可以，看前端逻辑)
    // 这里我们用 io.emit 确保完全一致（虽然会有一次回环，但 Vue 响应式能处理）
    socket.broadcast.emit("sync_state", globalGameState); 
  });

  // 3. 转发日志
  socket.on("send_log", (msg) => {
    socket.broadcast.emit("new_log", msg);
  });
});

console.log("🚀 JOJO TRPG Server running on port 3000");