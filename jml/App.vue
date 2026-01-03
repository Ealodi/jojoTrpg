<script>
    // 引入 store
    import { useGameStore } from '@/stores/game'

	export default {
		onLaunch: function() {
			console.log('App Launch')
            
            // 🚀 核心修改：APP 启动时初始化 Socket 连接
            // 这样无论你在哪个页面，连接都能保持
            const store = useGameStore()
            store.initSocket()
		},
		onShow: function() {
			console.log('App Show')
		},
		onHide: function() {
			console.log('App Hide')
		}
	}
</script>

<style>
	/*每个页面公共css */
    /* 可以在这里加一些全局样式，比如背景色，防止有白边 */
    page {
        background-color: #1a1a1a;
        color: #eee;
    }
</style>