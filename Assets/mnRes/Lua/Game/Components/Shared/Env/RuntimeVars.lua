--[[
运行期变量，不会被保存
]]

local RuntimeVars = {}

function RuntimeVars.Initialize()
	print('[RuntimeVars] initialize')
end

Env.RuntimeVars = RuntimeVars

require 'Game.Components.Shared.Env.Login_RuntimeVars'
require 'Game.Components.Shared.Env.Lobby_RuntimeVars'
require 'Game.Components.Shared.Env.Match_RuntimeVars'