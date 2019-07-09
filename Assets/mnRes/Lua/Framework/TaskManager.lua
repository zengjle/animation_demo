--[[
任务管理
]]

TaskManager = {}

local self = TaskManager

self.queue = {}
self.signals = {}
self.signalNum = 0
self.state = 'idle'
self.step = 0
self.progress = 0
self.factor = 0
self.worker = nil

-- 初始化系统
function TaskManager.Initialize()
	print('[TaskManager] initialize')
end

-- 关闭系统
function TaskManager.Shutdown()
	print('[TaskManager] shutdown')
end

-- 重置任务队列
function TaskManager.Reset()

	for i = 1, #self.queue do
		self.queue[i]:Cleanup()
	end

	self.queue = {}
	self.signals = {}
	self.signalNum = 0
	self.state = 'idle'
	self.step = 0
	self.progress = 0
	self.factor = 0

	if self.worker then
		CSCoroutine.Stop(self.worker)
		self.worker = nil
	end

	print('[TaskManager] reset')
end

-- 添加任务
function TaskManager.AddTask(task)
	if not task then
		return
	end 

	if task.__signature__ ~= '__task_signature__' then
		error('[TaskManager] add invalid task ' .. (task.name and task.name or '__invalid_task__'))
		return
	end

	self.queue[#self.queue + 1] = task
	self.factor = self.factor + ((task.factor and task.factor > 0) and task.factor or 0)

	print('[TaskManager] add new task ' .. task.name .. ', factor ' .. task.factor)
end

-- 开始任务
function TaskManager.Start()
	self.step = #self.queue > 0 and 1 or 0
	if self.step == 0 then
		warn('[TaskManager] queue of task is empty, confirm pls')
		return
	end

	self.AddTask(Task_LoadingEnd.New())

	self.state = 'running'
	self.worker = CSCoroutine.Start(function ()
		print('[TaskManager] worker start')

		local tasks = self.queue
		local total = #tasks

		coroutine.yield(Delay.wait500MilliSeconds)

		while self.step > 0 do
			local task = tasks[self.step]
			if task.state == 'idle' then
				task:Start()
			elseif task.state == 'running' then
				coroutine.yield(Delay.wait10MilliSeconds)
			elseif task.state == 'finished' then
				self.step = self.step + 1
				self.progress = self.progress + ((self.factor > 0) and (task.factor / self.factor) or 0)
				print('[TaskManager] broadcast ' .. self.progress)
				EventManager:Broadcast(EventTypes.System_TaskProgress, self.progress)
				if self.step > total then
					break
				end

				coroutine.yield(Delay.wait10MilliSeconds)
			else
				error('[TaskManager] worker fatal exception')
				break
			end
		end
	end)
end

-- 进度更新
function TaskManager.Progress(progress)
	print('[TaskManager] progress ' .. progress)
	if self.step > 0 and self.step <= #self.queue then
		local task = self.queue[self.step]
		progress = (progress and progress >= 0 and progress) <= 1 and progress or 0
		local total = self.progress + ((self.factor > 0) and ((task.factor * progress) / self.factor) or 0)

		print('[TaskManager] broadcast ' .. total)
		
		EventManager:Broadcast(EventTypes.System_TaskProgress, total)
	end
end

-- 重置信号量
function TaskManager.ResetSignal()
	self.signals = {}
	self.signalNum = 0
end

-- 注册信号
function TaskManager.RegisterSignal(name)
	local s = '' .. name

	if self.signals[s] then
		return
	end

	self.signals[s] = 0
end

-- 信号是否已经都完成
function TaskManager.SignalCompleted()
	return self.signalNum >= #self.signals
end

-- 信号完成
function TaskManager.Signal(name)
	local s = '' .. name

	if not self.signals[s] or self.signals[s] ~= 0 then
		return
	end 

	self.signals[s] = 1
	self.signalNum = self.signalNum + 1

	self.Progress(self.signalNum / #self.signals)

	-- 所有的信号已经完成
	if self.signalNum >= #self.signals then
		EventManager:Broadcast(EventTypes.System_AllSignalsFinish)
		self.ResetSignal()
	end
end

--[[
任务-结束载入过程
]]

Task_LoadingEnd = Class(Task)

function Task_LoadingEnd:ctor()
	self.name = 'loading end'
	self.factor = 0
end

function Task_LoadingEnd:OnStart()
	CSCoroutine.Start(function ()
		coroutine.yield(Delay.wait10MilliSeconds)
		self:OnFinished()
		TaskManager.Reset()
	end)	
end