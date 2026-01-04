import { defineStore } from 'pinia'
import { HubConnectionBuilder, HubConnection, HubConnectionState } from '@microsoft/signalr'
import { ref, computed } from 'vue'
import { ElMessage } from 'element-plus'

// 定义接口，必须与 C# 后端的 Models 保持一致
interface CharacterStats {
  name: string
  currentHp: number
  maxHp: number
  currentEnergy: number
  maxEnergy: number
  speed: number
  actions: number // [cite: 12]
}

interface Player {
  connectionId: string
  userId: string
  x: number
  y: number
  stats: CharacterStats
}

interface GameRoom {
  roomId: string
  players: Player[]
  currentTurn: number
  mapSize: number // [cite: 15]
}

export const useGameStore = defineStore('game', () => {
  // 状态 State
  const connection = ref<HubConnection | null>(null)
  const currentRoom = ref<GameRoom | null>(null)
  const isConnected = ref(false)
  const myUsername = ref('')
  const isReacting = ref(false)
  const reactionContext = ref({ attackerName: '', skillName: '' })

  // 计算属性 Getters
  // 获取当前登录玩家的对象
  const myPlayer = computed(() => 
    currentRoom.value?.players.find(p => p.userId === myUsername.value)
  )

  const checkConnection = (): boolean => {
    if (!connection.value || connection.value.state !== HubConnectionState.Connected) {
      ElMessage.warning('未连接到服务器，无法操作')
      return false
    }
    return true
  }

  // 动作 Actions
  const initConnection = async () => {
    // 指向你的 C# 后端地址 (注意端口号，VS2022 启动时通常是 5xxx 或 7xxx，请查看 launchSettings.json)
    const url = 'http://localhost:5009/gameHub' 

    const newConnection = new HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect()
      .build()

    // 1. 监听后端广播: RoomUpdated (对应 C# GameHub.cs 中的 SendAsync("RoomUpdated"))
    newConnection.on('RoomUpdated', (room: GameRoom) => {
      console.log('房间状态更新:', room)
      currentRoom.value = room
    })

    // 2. 监听日志消息
    newConnection.on('ReceiveLog', (msg: string) => {
      ElMessage.info(msg)
    })

    newConnection.on('RequestReaction', (attackerName: string, skillName: string) => {
      console.log("收到反应请求", attackerName, skillName)
      reactionContext.value = { attackerName, skillName }
      isReacting.value = true // 触发 UI 弹窗
   })

    try {
      await newConnection.start()
      console.log('SignalR 连接成功')
      isConnected.value = true
      connection.value = newConnection
    } catch (err) {
      console.error('SignalR 连接失败:', err)
      ElMessage.error('连接服务器失败，请检查后端是否启动')
    }
  }

  // 加入房间
  const joinRoom = async (roomId: string, username: string, charName: string) => {
    if (!connection.value) return
    myUsername.value = username
    // 调用 C# 方法: public async Task JoinRoom(...)
    await connection.value.invoke('JoinRoom', roomId, username, charName)
  }

  // 移动棋子
  const movePiece = async (x: number, y: number) => {
    if (!connection.value || !currentRoom.value) return
    // 调用 C# 方法: public async Task MovePiece(...)
    try {
      await connection.value.invoke('MovePiece', currentRoom.value.roomId, x, y)
    } catch (err) {
      console.error("移动失败", err)
    }
  }

  // 使用技能
  const useSkill = async (skillId: string, targetX: number, targetY: number) => {
    if (!connection.value || !currentRoom.value) return
    try {
      await connection.value.invoke('UseSkill', currentRoom.value.roomId, skillId, targetX, targetY)
    } catch (err) {
      console.error("技能释放失败", err)
      ElMessage.error("技能释放请求失败")
    }
  }

  // 发送反应选择
  const sendReaction = async (type: number) => {
    if (!checkConnection() || !currentRoom.value) return
    try {
        await connection.value!.invoke('SubmitReaction', currentRoom.value.roomId, type)
        isReacting.value = false // 关闭弹窗
    } catch (err) {
        console.error("反应提交失败", err)
    }
  }
  return {
    connection,
    currentRoom,
    isConnected,
    myPlayer,
    initConnection,
    joinRoom,
    movePiece,
    useSkill
  }
})