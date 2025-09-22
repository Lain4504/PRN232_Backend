# Database Entities

## Ads
```csharp
public class Ads
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Image { get; set; }
}
```

## Author
```csharp
public partial class Author
{
    public long Id { get; set; }
    public string Name { get; set; }
}
```

## AuthorBook
```csharp
public partial class AuthorBook
{
    public long BookId { get; set; }
    public long AuthorId { get; set; }
    public virtual Author Author { get; set; }
    public virtual Book Book { get; set; }
}
```

## Book
```csharp
public partial class Book
{
    public long Id { get; set; }
    public string Isbn { get; set; }
    public string Cover { get; set; }
    public string Description { get; set; }
    public float? Discount { get; set; }
    public int? Page { get; set; }
    public long? Price { get; set; }
    public DateOnly? PublicationDate { get; set; }
    public string Size { get; set; }
    public int? Sold { get; set; }
    public string State { get; set; }
    public int? Stock { get; set; }
    public string Title { get; set; }
    public int? Weight { get; set; }
    public long? PublisherId { get; set; }
    public virtual ICollection<Feedback> Feedbacks { get; set; }
    public virtual ICollection<Image> Images { get; set; }
    public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    public virtual Publisher Publisher { get; set; }
    public virtual ICollection<Wishlist> Wishlists { get; set; }
    public long? SalePrice { get; }
}
```

## BookCollection
```csharp
public partial class BookCollection
{
    public long BookId { get; set; }
    public long CollectionId { get; set; }
    public virtual Book Book { get; set; }
    public virtual Collection Collection { get; set; }
}
```

## Collection
```csharp
public partial class Collection
{
    public long Id { get; set; }
    public bool? IsDisplay { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
}
```

## Feedback
```csharp
public partial class Feedback
{
    public long Id { get; set; }
    public string Comment { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string State { get; set; }
    public long BookId { get; set; }
    public long UserId { get; set; }
    public virtual Book Book { get; set; }
    public virtual User User { get; set; }
}
```

## Image
```csharp
public partial class Image
{
    public long Id { get; set; }
    public string Description { get; set; }
    public string Link { get; set; }
    public long? BookId { get; set; }
    public virtual Book Book { get; set; }
}
```

## Order
```csharp
public partial class Order
{
    public long Id { get; set; }
    public string Address { get; set; }
    public DateTime? Created { get; set; }
    public string CustomerNote { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string PaymentState { get; set; }
    public string Phone { get; set; }
    public long? ShippingPrice { get; set; }
    public string ShippingState { get; set; }
    public string ShopNote { get; set; }
    public string State { get; set; }
    public long? TotalPrice { get; set; }
    public long? UserId { get; set; }
    public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    public virtual User User { get; set; }
}
```

## OrderDetail
```csharp
public partial class OrderDetail
{
    public long Id { get; set; }
    public int? Amount { get; set; }
    public long? OriginalPrice { get; set; }
    public long? SalePrice { get; set; }
    public long? BookId { get; set; }
    public long? OrderId { get; set; }
    public virtual Book Book { get; set; }
    public virtual Order Order { get; set; }
}
```

## Post
```csharp
public partial class Post
{
    public long Id { get; set; }
    public string Brief { get; set; }
    public string Content { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string State { get; set; }
    public string Thumbnail { get; set; }
    public string Title { get; set; }
    public long? CategoryId { get; set; }
    public long? UserId { get; set; }
    public virtual PostCategory Category { get; set; }
    public virtual User User { get; set; }
}
```

## PostCategory
```csharp
public partial class PostCategory
{
    public long Id { get; set; }
    public string Name { get; set; }
    public virtual ICollection<Post> Posts { get; set; }
}
```

## Publisher
```csharp
public partial class Publisher
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Website { get; set; }
    public virtual ICollection<Book> Books { get; set; }
}
```

## RefreshToken
```csharp
public class RefreshToken
{
    public long Id { get; set; }
    public string Token { get; set; }
    public long UserId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public bool Revoked { get; set; }
    public bool Used { get; set; }
    public virtual User User { get; set; }
}
```

## Slider
```csharp
public partial class Slider
{
    public long Id { get; set; }
    public string BackLink { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public string Title { get; set; }
}
```

## User
```csharp
public partial class User
{
    public long Id { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
    public string State { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public DateTime? Dob { get; set; }
    public string Gender { get; set; }
    public DateTime Created { get; set; }
    public virtual ICollection<Feedback> Feedbacks { get; set; }
    public virtual ICollection<Order> Orders { get; set; }
    public virtual ICollection<Post> Posts { get; set; }
    public virtual ICollection<Wishlist> Wishlists { get; set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; }
}
```

## Wishlist
```csharp
public partial class Wishlist
{
    public long Id { get; set; }
    public long? BookId { get; set; }
    public long? UserId { get; set; }
    public virtual Book Book { get; set; }
    public virtual User User { get; set; }
}