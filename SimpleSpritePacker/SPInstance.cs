using System.Collections.Generic;
using UnityEngine;

namespace SimpleSpritePacker;

public class SPInstance : ScriptableObject
{
	public enum PackingMethod
	{
		MaxRects,
		Unity
	}

	[SerializeField]
	private Texture2D m_Texture;

	[SerializeField]
	private int m_Padding = 1;

	[SerializeField]
	private int m_MaxSize = 8192;

	[SerializeField]
	private PackingMethod m_PackingMethod;

	[SerializeField]
	private SpriteAlignment m_DefaultPivot;

	[SerializeField]
	private Vector2 m_DefaultCustomPivot = new Vector2(0.5f, 0.5f);

	[SerializeField]
	private List<SPSpriteInfo> m_Sprites = new List<SPSpriteInfo>();

	[SerializeField]
	private List<SPAction> m_PendingActions = new List<SPAction>();

	public Texture2D texture
	{
		get
		{
			return m_Texture;
		}
		set
		{
			m_Texture = value;
		}
	}

	public int padding
	{
		get
		{
			return m_Padding;
		}
		set
		{
			m_Padding = value;
		}
	}

	public int maxSize
	{
		get
		{
			return m_MaxSize;
		}
		set
		{
			m_MaxSize = value;
		}
	}

	public PackingMethod packingMethod
	{
		get
		{
			return m_PackingMethod;
		}
		set
		{
			m_PackingMethod = value;
		}
	}

	public SpriteAlignment defaultPivot
	{
		get
		{
			return m_DefaultPivot;
		}
		set
		{
			m_DefaultPivot = value;
		}
	}

	public Vector2 defaultCustomPivot
	{
		get
		{
			return m_DefaultCustomPivot;
		}
		set
		{
			m_DefaultCustomPivot = value;
		}
	}

	public List<SPSpriteInfo> sprites => m_Sprites;

	public List<SPSpriteInfo> copyOfSprites
	{
		get
		{
			List<SPSpriteInfo> list = new List<SPSpriteInfo>();
			foreach (SPSpriteInfo sprite in m_Sprites)
			{
				list.Add(sprite);
			}
			return list;
		}
	}

	public List<SPAction> pendingActions => m_PendingActions;

	public void ChangeSpriteSource(SPSpriteInfo spriteInfo, Object newSource)
	{
		if (newSource == null)
		{
			spriteInfo.source = null;
		}
		else if (newSource is Texture2D || newSource is Sprite)
		{
			spriteInfo.source = newSource;
		}
	}

	public void QueueAction_AddSprite(Object resource)
	{
		if ((resource is Texture2D || resource is Sprite) && m_PendingActions.Find((SPAction a) => a.actionType == SPAction.ActionType.Sprite_Add && a.resource == resource) == null)
		{
			SPAction sPAction = new SPAction();
			sPAction.actionType = SPAction.ActionType.Sprite_Add;
			sPAction.resource = resource;
			m_PendingActions.Add(sPAction);
		}
	}

	public void QueueAction_AddSprites(Object[] resources)
	{
		foreach (Object resource in resources)
		{
			QueueAction_AddSprite(resource);
		}
	}

	public void QueueAction_RemoveSprite(SPSpriteInfo spriteInfo)
	{
		if (spriteInfo != null && m_Sprites.Contains(spriteInfo) && m_PendingActions.Find((SPAction a) => a.actionType == SPAction.ActionType.Sprite_Remove && a.spriteInfo == spriteInfo) == null)
		{
			SPAction sPAction = new SPAction();
			sPAction.actionType = SPAction.ActionType.Sprite_Remove;
			sPAction.spriteInfo = spriteInfo;
			m_PendingActions.Add(sPAction);
		}
	}

	public void UnqueueAction(SPAction action)
	{
		if (m_PendingActions.Contains(action))
		{
			m_PendingActions.Remove(action);
		}
	}

	protected List<SPAction> GetAddSpriteActions()
	{
		List<SPAction> list = new List<SPAction>();
		foreach (SPAction pendingAction in m_PendingActions)
		{
			if (pendingAction.actionType == SPAction.ActionType.Sprite_Add)
			{
				list.Add(pendingAction);
			}
		}
		return list;
	}

	protected List<SPAction> GetRemoveSpriteActions()
	{
		List<SPAction> list = new List<SPAction>();
		foreach (SPAction pendingAction in m_PendingActions)
		{
			if (pendingAction.actionType == SPAction.ActionType.Sprite_Remove)
			{
				list.Add(pendingAction);
			}
		}
		return list;
	}

	public void ClearSprites()
	{
		m_Sprites.Clear();
	}

	public void AddSprite(SPSpriteInfo spriteInfo)
	{
		if (spriteInfo != null)
		{
			m_Sprites.Add(spriteInfo);
		}
	}

	public void ClearActions()
	{
		m_PendingActions.Clear();
	}

	public List<SPSpriteInfo> GetSpriteListWithAppliedActions()
	{
		List<SPSpriteInfo> list = new List<SPSpriteInfo>();
		foreach (SPSpriteInfo sprite in m_Sprites)
		{
			list.Add(sprite);
		}
		foreach (SPAction removeSpriteAction in GetRemoveSpriteActions())
		{
			if (list.Contains(removeSpriteAction.spriteInfo))
			{
				list.Remove(removeSpriteAction.spriteInfo);
			}
		}
		foreach (SPAction addSpriteAction in GetAddSpriteActions())
		{
			SPSpriteInfo sPSpriteInfo = new SPSpriteInfo();
			sPSpriteInfo.source = addSpriteAction.resource;
			list.Add(sPSpriteInfo);
		}
		return list;
	}
}
