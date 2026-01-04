<template>
  <div class="login-container">
    <el-card class="login-card">
      <template #header>
        <div class="card-header">
          <span>JOJO 桌游 - 联机大厅</span>
        </div>
      </template>
      
      <el-form label-width="80px">
        <el-form-item label="用户名">
          <el-input v-model="username" placeholder="请输入你的名字" />
        </el-form-item>
        
        <el-form-item label="房间号">
          <el-input v-model="roomId" placeholder="例如: ROOM1" />
        </el-form-item>

        <el-form-item label="选择角色">
          <el-select v-model="selectedChar" placeholder="请选择替身">
            <el-option label="空条承太郎 (白金之星)" value="承太郎" />
            <el-option label="DIO (世界)" value="DIO" />
            <el-option label="乔鲁诺 (黄金体验)" value="茸茸" />
            <el-option label="波鲁纳雷夫 (银色战车)" value="波波" />
            <el-option label="阿布德尔 (魔术师之红)" value="阿布德尔" />
            <el-option label="花京院 (法皇之绿)" value="花京院" />
            <el-option label="米斯达 (性感手枪)" value="米斯达" />
            <el-option label="天气预报" value="天气预报" />
          </el-select>
        </el-form-item>

        <el-button type="primary" class="w-100" @click="handleJoin" :disabled="!store.isConnected">
          {{ store.isConnected ? '进入游戏' : '正在连接服务器...' }}
        </el-button>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useGameStore } from '../stores/gameStore'

const store = useGameStore()
const username = ref('')
const roomId = ref('ROOM1')
const selectedChar = ref('承太郎')

onMounted(() => {
  store.initConnection()
})

const handleJoin = () => {
  if (username.value && roomId.value) {
    store.joinRoom(roomId.value, username.value, selectedChar.value)
  }
}
</script>

<style scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
  background-color: #2c3e50;
}
.login-card {
  width: 400px;
}
.w-100 {
  width: 100%;
}
</style>