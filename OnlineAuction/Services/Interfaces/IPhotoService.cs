namespace OnlineAuction.Services.Interfaces;

public interface IPhotoService
{
    // Upload 1 anh len Cloudinary va tra ve URL bao mat (https).
    // Service CRUD chi can URL nay de luu vao cot products.primary_image.
    Task<string?> AddPhotoAsync(IFormFile? file, string folder);
}
