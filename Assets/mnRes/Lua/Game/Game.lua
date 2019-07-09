local Game = Component('__game__', Config.ViewConstants.top, nil, nil, nil, 'Game')

function Game.SceneLoadFinished(name)
    print('[Game] scene ' .. name .. ' entered')
end

function Game:OnEnable()
	print('[Game] game OnEnable')

    UnityEngine.Application.targetFrameRate = Constants.fps
    UnityEngine.Screen.sleepTimeout = UnityEngine.SleepTimeout.NeverSleep

	self.startup = true

	-- SharedSystems.Initialize()

	-- 初始化一些共享的功能
	self.Shared:Initialize()

	--setPrintStack(true)

	print('[Game] name ' .. self.model.name .. ', version ' .. self.model.version)

	-- 创建游戏情景
	ViewManager.CreateContext('Login', 'Assets/Scenes/Login.unity', self.Login)
	ViewManager.CreateContext('Lobby', 'Assets/Scenes/Lobby.unity', self.Lobby)

	-- 激活shared组件
	self.Shared:Active(true)
	-- 登录画面
	print("before login active")
	self.Login:Active(true, function ()
	end)
	print("end login active")
end

return Game