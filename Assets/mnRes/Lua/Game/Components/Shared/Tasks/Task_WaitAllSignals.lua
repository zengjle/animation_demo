--[[
任务-等待所有信号完成
]]

Task_WaitAllSignals = Class(Task)

function Task_WaitAllSignals:ctor(factor)
    self.name = 'wait all signals'
    self.factor = factor
end

function Task_WaitAllSignals:OnStart()
    if TaskManager.SignalCompleted() then
        self:OnFinished()
    else
        EventManager:AddListener(EventTypes.System_AllSignalsFinish, Task_WaitAllSignals.OnSignalsFinish, self)
    end
end

function Task_WaitAllSignals:OnSignalsFinish()
    self:OnFinished()
end