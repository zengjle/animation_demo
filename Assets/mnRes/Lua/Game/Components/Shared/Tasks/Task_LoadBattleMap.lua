--[[
任务-载入战斗地图
]]

Task_LoadBattleMap = Class(Task)

function Task_LoadBattleMap:ctor(factor, mapId)
	self.name = 'load battle map'
	self.mapId = mapId
end

function Task_LoadBattleMap:OnStart()
	BattleDC.LoadMap(self.mapId, false, function (entity)
		self:OnFinished()
	end)
end