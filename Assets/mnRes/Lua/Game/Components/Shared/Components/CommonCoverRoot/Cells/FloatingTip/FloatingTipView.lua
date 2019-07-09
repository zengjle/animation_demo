--[[
    浮动tip view
]]

local FloatingTipView = {}

FloatingTipView.args = {
    txtDes = Types.Text,
    tweenAnimation = Types.DOTweenAnimation
}
FloatingTipView.co = nil
local args = FloatingTipView.args
local position = nil

function FloatingTipView:OnEnable()
    print('[FloatingTipView] --> OnEnable')
end

function FloatingTipView:ShowMessage()
    print('[FloatingTipView] --> ShowMessage()')
    args.txtDes.text = self.controller:GetData()
    args.tweenAnimation:DOPlay()
end

--一次动画播放完毕, 回到原位并disable掉
function FloatingTipView:OnPlayComplete()
    print('[FloatingTipView] OnPlayComplete start')
    local cells = Game.Shared.CommonCoverRoot.cells
    args.tweenAnimation:DORewind()
    cells.FloatingTip:Free(self.controller)
end

return FloatingTipView