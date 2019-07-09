--[[
栈结构，但提供一些辅助性额外的接口，比如Remove指定节点
]]

Stack = Class()

function Stack:ctor(name)
	self.name = name
	--print('------------------- stack name ' .. name)
end

-- 压入一个数据
function Stack:Push(data)
	if not data then
		warn("[Stack] stack push a nil data")
		return
	end

	self[#self + 1] = data

	--print('------------------- stack add ' .. self.name .. ' count ' .. #self)
end

-- 弹出栈顶
function Stack:Pop()
	local data = self:Top()
	if not data then
		return
	end

	table.remove(self, #self)
	return data 
end

-- 删除指定数据
function Stack:Remove(data)
	--print('------------------- stack before remove ' .. self.name .. ' count ' .. #self)

	for i = #self, 1, -1 do
		if data == self[i] then
			table.remove(self, i)
			--print('------------------- stack after remove ' .. self.name .. ' count ' .. #self)
			return
		end
	end
end

-- 返回栈的数量
function Stack:Count()
	return #self
end

-- 弹出所有数据，并支持数据处理
function Stack:PopAll(handler)
	local data = self:Pop()

	while data do
		if handler then
			handler(data)
		end

		data = self:Pop()
	end
end

-- 遍历栈，支持数据处理
function Stack:Traverse(handler)
	if not handler then
		return
	end

	for i = #self, 1, -1 do
		handler(self[i], #self - i + 1)
	end
end

-- 返回栈顶，无弹出操作
function Stack:Top()
	local data = self[#self]
	if not data then
		return nil
	end

	return data
end

-- 清空栈
function Stack:Clear()
	self = {}
end

-- 打印数据
function Stack:Dump(printElement)
	print("[Stack] stack begin, num = " .. #self)
	
	for i = #self, 1, -1 do
		print("\t[Stack] [" .. i .. "] = " .. (printElement and printElement(self[i]) or tostring(self[i])))
	end
	
	print("[Stack] stack end")
end