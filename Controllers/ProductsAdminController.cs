using HolyWater.Server.Interfaces;
using HolyWater.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
namespace HolyWater.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsAdminController : ControllerBase
    {
        readonly IProductsAdminService _productsAdminService;
        public ProductsAdminController(IProductsAdminService productsAdminService)
        {
            _productsAdminService = productsAdminService;
        }

        [HttpGet("get-All-Products")]
        public IActionResult GetProduct()
        {
            var products = _productsAdminService.GetAllProducts();
            return Ok(products);
        }

        [HttpGet("getProductById")]
        public IActionResult GetProduct([FromQuery] int id)
        {
            var products = _productsAdminService.GetProductById(id);
            return Ok(products);
        }

        [HttpDelete("deleteProductById")]
        public IActionResult RemoveProduct([FromQuery] int id)
        {
            _productsAdminService.DeleteProduct(id);
            return Ok();
        }

        [HttpPut("updateProduct-with-image")]
        public IActionResult UpdateProduct([FromForm] ProductDTO product, [FromQuery] int id)
        {
            string imagePath = string.Empty;
                

            if (product.Image != null && product.Image.Length > 0)
            {
                imagePath = CreatedImagePath(product);
            }

            var prod = new Product()
            {
                Name = product.Name,
                Title = product.Title,
                Price = (decimal)product.Price,
                OldPrice = (decimal)product.OldPrice,
                Category = product.Category,
                Description = product.Description,
                InStock = product.InStock,
                Onsale = product.OnSale,
                Qty = product.Qty,
                Image = imagePath
            };
            _productsAdminService.UpdateProduct(prod, id);
            var products = _productsAdminService.GetAllProducts();
            return Ok(products);
        }

        [HttpPost("AddProduct-with-image")]
        public IActionResult AddProduct([FromForm] ProductDTO product)
        {
            string imagePath = string.Empty;
            if (product.Image != null && product.Image.Length > 0)
            {
                imagePath = CreatedImagePath(product);
            }

            var prod = new Product()
            {
                Name = product.Name,
                Title = product.Title,
                Price = (decimal)product.Price,
                OldPrice = (decimal)product.OldPrice,
                Category = product.Category,
                Description = product.Description,
                InStock = product.InStock,
                Onsale = product.OnSale,
                Qty = product.Qty,
                Image = imagePath
            };
            _productsAdminService.AddProduct(prod);
            return Ok();
        }

        private string CreatedImagePath(ProductDTO product)
        {
            var uniqueFileName = string.Empty;
            if (product.Image != null && product.Image.Length > 0)
            {
                // Ensure the "wwwroot/images" directory exists
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Generate a unique name to avoid conflicts
                uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(product.Image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the uploaded file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    product.Image.CopyTo(fileStream);
                }

                // Save a relative path (used in frontend)

            }
            return $"/images/{uniqueFileName}";
        }
    }
}
