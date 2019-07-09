--[[
序列-执行
]]

Sequence_Exec = Class(Sequence)

function Task_Exec:ctor(factor, func)
    self.name = 'exec'
    self.func = func
end

function Task_Exec:OnStart()
    if self.func then
        self.func()
    end

    self:OnFinished()
end
