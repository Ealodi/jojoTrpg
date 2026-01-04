<template>
  <div class="game-layout" v-if="store.currentRoom">
    <div class="map-container">
      <div class="grid-map" :style="gridStyle">
        <div 
          v-for="index in totalCells" 
          :key="index"
          class="cell"
          :class="{ 'highlight': isMovable(index) }"
          @click="handleCellClick(index)"
        >
          <span class="coord-debug">{{ getX(index) }},{{ getY(index) }}</span>
          
          <div 
            v-if="getPlayerAt(index)" 
            class="player-token"
            :class="{ 'is-me': getPlayerAt(index)?.userId === store.myPlayer?.userId }"
          >
            {{ getPlayerAt(index)?.stats.name[0] }} <div class="hp-bar">
               {{ getPlayerAt(index)?.stats.currentHp }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="status-panel">
      <h3>房间: {{ store.currentRoom.roomId }}</h3>
      
      <div v-if="store.myPlayer" class="my-stats">
        <h4>{{ store.myPlayer.stats.name }} (我)</h4>
        <p>HP: {{ store.myPlayer.stats.currentHp }} / {{ store.myPlayer.stats.maxHp }}</p>
        <p>能量: {{ store.myPlayer.stats.currentEnergy }} / {{ store.myPlayer.stats.maxEnergy }}</p>
        <p>动作: {{ store.myPlayer.stats.actions }}</p>
        <p>位置: {{ store.myPlayer.x }}, {{ store.myPlayer.y }}</p>
      </div>
      
      <el-divider />
      <h4>当前玩家列表:</h4>
      <ul>
        <li v-for="p in store.currentRoom.players" :key="p.connectionId">
           {{ p.userId }} - {{ p.stats.name }} ({{ p.x }},{{ p.y }})
        </li>
      </ul>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useGameStore } from '../stores/gameStore'

const store = useGameStore()

// 假设地图大小是 20x20
const mapSize = computed(() => store.currentRoom?.mapSize || 20)
const totalCells = computed(() => mapSize.value * mapSize.value)

// 动态 CSS Grid 样式
const gridStyle = computed(() => ({
  gridTemplateColumns: `repeat(${mapSize.value}, 1fr)`,
  gridTemplateRows: `repeat(${mapSize.value}, 1fr)`
}))

// 工具函数：根据索引计算坐标 (从0开始)
const getX = (index: number) => (index - 1) % mapSize.value
const getY = (index: number) => Math.floor((index - 1) / mapSize.value)

// 获取某格子的玩家
const getPlayerAt = (index: number) => {
  const x = getX(index)
  const y = getY(index)
  return store.currentRoom?.players.find(p => p.x === x && p.y === y)
}

// 简单判断是否在移动范围内 (曼哈顿距离)
const isMovable = (index: number) => {
  if (!store.myPlayer) return false
  const x = getX(index)
  const y = getY(index)
  const dist = Math.abs(store.myPlayer.x - x) + Math.abs(store.myPlayer.y - y)
  // [cite: 18] 速度决定移动距离
  return dist <= store.myPlayer.stats.speed && dist > 0
}

const handleCellClick = (index: number) => {
  const x = getX(index)
  const y = getY(index)
  
  // 如果点击的是自己可移动的范围，发送移动请求
  if (isMovable(index)) {
    store.movePiece(x, y)
  }
}
</script>

<style scoped>
.game-layout {
  display: flex;
  height: 100vh;
}
.map-container {
  flex: 3;
  background: #333;
  padding: 20px;
  display: flex;
  justify-content: center;
  align-items: center;
}
.grid-map {
  display: grid;
  width: 600px;
  height: 600px;
  background: #fff;
  gap: 1px;
  border: 5px solid #444;
}
.cell {
  background: #eee;
  position: relative;
  cursor: pointer;
  display: flex;
  justify-content: center;
  align-items: center;
}
.cell:hover {
  background: #ddd;
}
.cell.highlight {
  background: #a8d8ea; /* 蓝色高亮移动范围 */
}
.player-token {
  width: 80%;
  height: 80%;
  border-radius: 50%;
  background: #e74c3c;
  color: white;
  display: flex;
  justify-content: center;
  align-items: center;
  font-weight: bold;
  position: relative;
  box-shadow: 2px 2px 5px rgba(0,0,0,0.3);
}
.player-token.is-me {
  background: #2ecc71; /* 自己是绿色 */
  border: 2px solid #fff;
}
.hp-bar {
  position: absolute;
  bottom: -5px;
  font-size: 10px;
  background: black;
  color: white;
  padding: 0 4px;
  border-radius: 4px;
}
.coord-debug {
  position: absolute;
  top: 1px;
  left: 1px;
  font-size: 8px;
  color: #ccc;
}
.status-panel {
  flex: 1;
  background: #fdfdfd;
  padding: 20px;
  border-left: 1px solid #ddd;
}
</style>