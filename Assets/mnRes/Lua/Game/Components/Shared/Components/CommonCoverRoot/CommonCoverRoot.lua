--[[
    多种需要挂载在Cover层的根节点
]]

local CommonCoverRoot = Component('__cover_root__', Config.ViewConstants.top, 'Shared/CommonCoverRoot/CommonCoverRootView', 'Cover')

function CommonCoverRoot:OnEnable()
end

function CommonCoverRoot:OnDisable()
end

-- 添加自定义功能根节点
function CommonCoverRoot:AddUserDefinedRoot(name)
	local gameObject = self.view.gameObject
    local node = gameObject.transform:Find(name)
    
    if node then
        warn('[CommonCoverRoot] AddUserDefinedRoot there is already exist ' .. name)
        return 
    end

    node = UnityEngine.GameObject(name)
    node.transform:SetParent(gameObject.transform)
    node.transform.localScale = UnityEngine.Vector3.one
    node.transform.localPosition = UnityEngine.Vector3.zero
end

return CommonCoverRoot