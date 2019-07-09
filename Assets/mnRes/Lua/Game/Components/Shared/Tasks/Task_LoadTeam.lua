--[[
任务-载入队伍
]]

Task_LoadTeam = Class(Task)

function Task_LoadTeam:ctor(factor, charIds, team)
	self.name = 'load team'
	self.charIds = charIds
	self.index = 1
	self.team = team
end

function Task_LoadTeam:OnStart()
	if #self.charIds == 0 then
		self:OnFinished()
	end

	Coroutine.Start(function ()
		while self.index <= #self.charIds do
			local loaded = false
			CharacterDC.CreateCharacter(self.charIds[self.index], false, self.team, self.index, function (entity)
				loaded = true
			end)

			while not loaded do
				Coroutine.Step()
			end

			self.index = self.index + 1
		end

		self:OnFinished()
	end)
end