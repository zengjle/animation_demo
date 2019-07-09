--[[
    浮动提示框
]]

local FloatingTip = Cell('__floating_tip__', 'Shared/CommonCoverRoot/TipCell/TipCellView', 'FloatingTip', true)
FloatingTip.message = nil 

function FloatingTip:ctor()
end

function FloatingTip:OnEnable()
    print('[FloatingTip] --> OnEnable')
    
end

function FloatingTip:SetData(message)
    self.message = message
end

function FloatingTip:GetData(message)
	return self.message
end

function FloatingTip:OnViewLoaded()
    print('[FloatingTip] --> OnViewLoaded')
    self.view:ShowMessage()
end

return FloatingTip