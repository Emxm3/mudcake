using MudCake.core.Pages.Nav;

namespace MudCake.Data.Infrastructure
{
    public class NavigationStructure : INavigationStructure
    {
        public string? Id { get; set; }
        public IContainer<INavigationData?, string?>? Parent { get; set; }
       
        public List<IContainer<INavigationData?, string>?>? Children { get; set; }
        public List<INavigationData?>? Content { get; set; }

        public void Arrange(string[] location, INavigationData? content) // Initial: Animals/pets/cats
        {
               if(location.Length > 1) //Children exist
            {
                if (Children == null) Children = new();

                //recurse

                Id = location[0]; //Animals

                var subLocation = location.Skip(1).ToArray();  // pets/cats
                string childName = subLocation[0];

                // Find the child and modify it
                if (Children.Select(c => c.Id).Any(c => c == childName))
                {
                    var child = Children.First(c => c.Id == childName);
                    ((INavigationStructure?)child)!.Arrange(subLocation, content);
                }
                else
                {
                    //Add non-existant child                                                           
                    INavigationStructure subNavigation = new NavigationStructure
                    {
                        Parent = this,
                        Id = childName
                    };

                    Children.Add(subNavigation!);
                    subNavigation.Arrange(subLocation, content);
                }              

            }
            else
            {
                //end of the chain
                Id = location[0];

                Content ??= [];
                Content.Add(content);
            }
        }
    }

    public interface INavigationStructure: IContainer<INavigationData?, string?>
    {


        /// <summary>
        /// When given a path like "Animals/pets/cats" sets location to "Animals" and pushes the rest to the child
        /// The final child will contain the content. This function ripples content wher eit needs to end up
        /// </summary>
        /// <param name="location"></param>
        /// <param name="content">The content at the end of the navigation data</param>
        void Arrange(string[] location, INavigationData? content);

    }
}
