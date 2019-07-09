--[[
任务的基本模式
]]

Task = Class()

Task.__signature__ = '__task_signature__'
Task.name = '__undefined_task__'
Task.factor = 1
Task.state = 'idle'

-- 任务启动
function Task:Start()
	print('[Task] task {' .. self.name .. '} startup')
	self.state = 'running'
	self:OnStart()
end

function Task:OnStart()
	error('[Task] task {' .. self.name .. '} redefine OnStart pls')
end

-- 清理任务
function Task:Cleanup()
	print('[Task] task {' .. self.name .. '} cleanup')
end

function Task:OnFinished()
	self.state = 'finished'
	print('[Task] task {' .. self.name .. '} finished')
end

function Task:OnProgress(progress)
	TaskManager.Progress(progress)
end
