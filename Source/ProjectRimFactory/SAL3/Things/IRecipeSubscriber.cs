namespace ProjectRimFactory.SAL3.Things;

public interface IRecipeSubscriber
{
    public void RecipesChanged(Building_RecipeHolder buildingRecipeHolder);
    
    public void RecipeProviderRemoved(Building_RecipeHolder buildingRecipeHolder);
}