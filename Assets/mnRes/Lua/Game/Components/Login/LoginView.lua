--[[
 登录界面
]]

local LoginView = {}

LoginView.args = {
	imgBg = Types.Image,
	txtVersion = Types.Text,
	btnEnter = Types.Button,
}

local args = LoginView.args

function LoginView:Awake()
	args.btnEnter.onClick:AddListener(function ()
		self.controller:EnterLobby()
	end)
end

function LoginView:Start()
	args.txtVersion.text = 'Version ' .. Constants.gameVersion
end

function LoginView:OnEnable()
end

function LoginView:OnDisable()
end

return LoginView