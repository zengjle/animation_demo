--[[
GUI相关辅助功能
]]

-- 添加自定义功能根节点
Helper.AddUserDefinedRoot = function (name)
	Game.Shared.CommonCoverRoot:AddUserDefinedRoot(name)
end

Helper.AddCommonPopupRoot = function (name)
    Game.Shared.CommonPopupRoot:AddUserDefinedRoot(name)
end

-- 弹出一个浮动提示框
Helper.AddTipView = function (message)
	local cells = Game.Shared.CommonCoverRoot.cells

	cells.FloatingTip:Add(1, function(item, id)
		item:SetData(message)
		item:StartRendered()
	end)
end
