using System.Text;
using System.Xml.Linq;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;

namespace Trading;

public abstract class ActionData : IChecksum
{
	public abstract string XElementName { get; }

	public abstract bool Execute<T>(T t, Entity player) where T : IEvaluable;

	public abstract bool Execute(ref GasMixture tradable, int totalQuantitySold);

	public virtual void Initialize()
	{
	}

	public virtual void ToolTip(StringBuilder stringBuilder, int generations, Thing prefab = null)
	{
	}

	public abstract int GetChecksum();

	public void Execute(IEvaluable tradable)
	{
		Execute(tradable as DynamicThing, null);
	}

	public abstract void Add(ref XElement parentElement, string actionElementName);
}
