--[[
    waiting菊花view
]]

local WaitingView = {}

WaitingView.args = {
     imgBack = Types.GameObject,
   
}

local args = WaitingView.args

WaitingView.co = nil

function WaitingView:OnEnable()
end

function WaitingView:OnDisable()
end

function WaitingView:StartLoading(time)
    Coroutine.Start(function()
        Coroutine.Wait(3)
        Game.Shared.Loading:Deactive(true)
    end)

end

function WaitingView:OnPlayComplete()
    print('this is DoTween  finished')
end

return WaitingView