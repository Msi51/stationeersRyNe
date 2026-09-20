namespace Assets.Scripts.Util;

public class RecipeComparable
{
	protected string RecipeName;

	public int InitialRecipeHash;

	public int CurrentRecipeHash;

	public string CompressedRecipe;

	public RecipeComparable()
	{
	}

	public RecipeComparable(string recipeName)
	{
		RecipeName = recipeName;
	}

	public bool IsDirty()
	{
		if (InitialRecipeHash != 0)
		{
			return InitialRecipeHash != CurrentRecipeHash;
		}
		return true;
	}

	public bool IsDirty(int hash)
	{
		if (CurrentRecipeHash != 0)
		{
			return CurrentRecipeHash != hash;
		}
		return true;
	}

	public virtual string GenerateCompressedRecipe()
	{
		return CompressedRecipe;
	}

	public virtual bool UpdatedRecipe(string compressedRecipe)
	{
		return true;
	}

	public virtual void RecoverRecipe()
	{
	}

	public virtual void ClearRecipe(bool resetInitialHash = false)
	{
		if (resetInitialHash)
		{
			InitialRecipeHash = 0;
		}
		CompressedRecipe = null;
		CurrentRecipeHash = 0;
	}

	public void SetInitialHash()
	{
		if (InitialRecipeHash == 0)
		{
			InitialRecipeHash = CurrentRecipeHash;
		}
	}

	public virtual void GenerateRecipieList()
	{
		GenerateRecipeHash();
		SetInitialHash();
	}

	public virtual void GenerateRecipeHash()
	{
	}
}
