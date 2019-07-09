--[[
    多种需要挂载在Popup层的根节点
]]

local CommonPopupRoot = Component('__popou_root__', Config.ViewConstants.top, 'Shared/CommonPopupRoot/CommonPopupRootView', 'Popup')

function CommonPopupRoot:OnEnable()
end

function CommonPopupRoot:OnDisable()
end

-- 添加自定义功能根节点
function CommonPopupRoot:AddUserDefinedRoot(name)
	local gameObject = self.view.gameObject
    local node = gameObject.transform:Find(name)
    
    if node then
        warn('[CommonPopupRoot] AddUserDefinedRoot there is already exist ' .. name)
        return 
    end

    node = UnityEngine.GameObject(name)
    node.transform:SetParent(gameObject.transform)
    node.transform.localScale = UnityEngine.Vector3.one
    node.transform.localPosition = UnityEngine.Vector3.zero
end

return CommonPopupRoot