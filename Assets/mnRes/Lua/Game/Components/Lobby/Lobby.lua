--[[
 大厅
]]

local Lobby = Component('__lobby__', Config.ViewConstants.top, 'Lobby/LobbyView', 'Base')

function Lobby:OnEnable()
    self:Element("gameBattle","gameBattle/gameBattleView")
    self:Element("uiRole","uiRole/uiRoleView")
end

function Lobby:OnViewLoaded()
end

function Lobby:BackToLogin()
    Helper.Switch2Login()
end

return Lobby