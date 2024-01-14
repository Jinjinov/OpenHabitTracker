using Ididit.Data;

namespace Ididit.Services;

public class CategoryService(IDataAccess dataAccess)
{
    private readonly IDataAccess _dataAccess = dataAccess;
}
