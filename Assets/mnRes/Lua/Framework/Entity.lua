--[[
 Entity，是一种包含Block的容器，可以用于组织功能模块
]]

function Entity (name)
	local entity = {}

	entity.name = name
	entity.blocks = {}
	entity.hasDestroyed = false
	entity.tags = {}

	-- 检查是否存在Block
	entity.HasBlock = function (self, block)
		if type(block) == 'string' then
			return self.blocks[block] ~= nil
		elseif type(block) == 'table' and block.__signature__ == blockSignature then
			return self.blocks[block.blockName] ~= nil
		end

		return false
	end

	-- 检查是否拥有block列表
	entity.hasBlocks = function (self, blocks)
		for i = 1, #blocks do
			if not self[blocks[i].blockName] then
				return false
			end
		end

		return true
	end

	-- 删除
	entity.Destroy = function (self)
		for i = 1, #self.blocks do 
			local b = self.blocks[i]
			if b and self[b.blockName] then
				self[b.blockName]:destroy()
				self.blocks[b.blockName] = nil
				self[b.blockName] = nil
			end
		end

		self.hasDestroyed = true
		self.blocks = {}
	end

	-- 添加block
	entity.AddBlock = function (self, block, tag)
		if block.__signature__ ~= blockSignature then
			error('it is not a block, confirm pls')
			return
		end

		if self[block.blockName] then
			error('block ' .. block.blockName .. ' has existed, confirm pls')
			return
		end

		rawset(block, 'entity', self)

		self[block.blockName] = block
		self.blocks[#self.blocks + 1] = block

		if tag then
			if not self.tags[tag] then
				self.tags[tag] = block
			else
				warn('tag ' .. tag .. ' has exist, confirm pls')
			end
		end

		block:active()
	end

	-- 删除block
	entity.RemoveBlock = function (self, block)
		if block.__signature__ ~= blockSignature then
			error('it is not a block, confirm pls')
			return
		end

		if not self[block.blockName] then
			return
		end

		local lastBlock = self[block.blockName]

		self[block.blockName] = nil

		for i = 1, #self.blocks do 
			if self.blocks[i]:TypeOf(block) then
				table.remove(self.blocks, i)
				return
			end
		end

		for k, v in pairs(self.tags) do
			if v:TypeOf(block) then
				self.tags[k] = nil
				break
			end
		end

		lastBlock:destroy()
	end

	setmetatable(entity, {
		__newindex = function (t, k, v)
			if k == 'destroyed' then
				if v then
					BlockManager.onTryEntityDestroy(t)
				end
				
				return
			end

			rawset(t, k, v)
		end
	})

	return entity
end