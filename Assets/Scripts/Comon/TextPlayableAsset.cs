using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

[System.Serializable]
public class TextPlayableAsset : PlayableAsset
{

    public ExposedReference<Text> m_DialogContainer;
    public string m_DialogStr;

    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<TextPlayableBehaviour>.Create(graph);
        playable.GetBehaviour().m_DialogContainer = m_DialogContainer.Resolve(graph.GetResolver());
        playable.GetBehaviour().m_DialogStr = m_DialogStr;
        return playable;
    }
}
