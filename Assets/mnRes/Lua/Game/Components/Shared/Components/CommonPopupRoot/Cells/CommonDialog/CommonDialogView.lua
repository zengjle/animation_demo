--[[
    弹出对话框 view
]]

local CommonDialogView = {}

CommonDialogView.args = {
	txtTip = Types.Text,
	btnClose = Types.Button,
}

local args = CommonDialogView.args

function CommonDialogView:Start()
    args.btnClose.onClick:AddListener(function()
        local cells = Game.Shared.CommonPopupRoot.cells

        cells.CommonDialog:Free(self.controller)
    end)
    
    self:ShowPopMessage()
end

function CommonDialogView:ShowPopMessage()
	args.txtTip.text = self.controller:GetData()
end

function CommonDialogView:OnDisable()
end

return CommonDialogView