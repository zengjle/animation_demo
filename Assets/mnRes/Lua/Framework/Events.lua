--[[
事件系统
]]

local EventLib = require "3rd.eventlib"

Event = Class()

Event.name = '__undefined_event_manager__'
Event.events = {}
Event.filter = nil

function Event:ctor(name, filter)
	self.name = name
	self.filter = filter
end

-- 添加监听者
function Event:AddListener(event, handler, from)
	print('监听:'..event)
	if not event or type(event) ~= "string" then
		error('[Event] event manager {' .. self.name .. '} parameter in addlistener function has to be string, ' .. type(event) .. ' not right.')
	end

	if not handler or type(handler) ~= "function" then
		error('[Event] event manager {' .. self.name .. '} handler parameter in addlistener function has to be function, '.. type(handler) .. ' not right')
	end

	if not self.events[event] then
		self.events[event] = EventLib:new(event)
	end

	self.events[event]:connect(handler, from)
end

-- 广播
function Event:Broadcast(event, ...)
	if not self.events[event] then
		return
	else
		self.events[event]:fire(nil, ...)
	end
end

-- 广播，并过滤掉
function Event:BroadcastExcept(event, exceptList, ...)
	if not self.events[event] then
		return
	else
		self.events[event]:fire(function (target)
			for i = 1, #exceptList do
				if target.from and target.from == exceptList[i] then
					return true
				end
			end

			return false
		end, ...)
	end
end

-- 删除监听者
function Event:RemoveListener(event, handler, from)
	if not self.events[event] then
		error('[Event] event manager {' .. self.name .. '} remove ' .. event .. ' has no event.')
	else
		self.events[event]:disconnect(handler, from)
	end
end
