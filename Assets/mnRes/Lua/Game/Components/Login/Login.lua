--[[
 登录
]]

local Login = Component('__login__', Config.ViewConstants.top, 'Login/LoginView', 'Base')

function Login:OnEnable()
end

function Login:OnViewLoaded()
end

function Login:EnterLobby()
    Helper.Switch2Lobby()
end

return Login