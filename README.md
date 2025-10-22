移动黑市商人到基地，地图中的不会消失，前置：需要在黑市商人所在地图待5秒

以下情况需要重新做一遍前置：
1.取消过本mod的开关
2.退出游戏了

原理概述：
进入地图5秒后会去寻找黑市商人，找到之后保存起来，当检测到基地场景加载完毕后，延迟2秒，在角落里加载保存过的黑市商人
黑市商人基地里被打死后，1秒后复活并刷新商店物品
地毯人无法被打死，只能通过切换地图来刷新商店

mod模板参考：https://github.com/xvrsl/duckov_modding

# 调试工具
BepInEx：https://github.com/BepInEx/BepInEx
UnityExplorer：https://github.com/yukieiji/UnityExplorer

# 调用库
Harmony库：https://github.com/pardeike/Harmony
