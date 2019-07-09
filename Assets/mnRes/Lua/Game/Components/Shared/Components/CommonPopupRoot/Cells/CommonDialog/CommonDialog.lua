--[[
    临时的公用弹出对话框
--]]

local CommonDialog = Cell('__common_dialog__', 'Shared/CommonDialog/CommonDialogView', 'CommonDialog', true)
CommonDialog.message = nil

function CommonDialog:ctor()
end

function CommonDialog:SetDialogMessage(message)
    self.view:ShowPopMessage(message)
end

function CommonDialog:SetData(message)
    self.message = message
end

function CommonDialog:GetData(message)
	return self.message
end

return CommonDialog