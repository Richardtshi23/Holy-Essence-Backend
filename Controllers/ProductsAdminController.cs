using HolyWater.Server.Interfaces;
using HolyWater.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
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
            var product = _productsAdminService.GetProductById(id);
            return Ok(product);
        }

        [HttpDelete("deleteProductById")]
        public IActionResult RemoveProduct([FromQuery] int id)
        {
            _productsAdminService.DeleteProduct(id);
            return Ok();
        }

        [HttpPut("updateProduct-with-image")]
        public async Task<IActionResult> UpdateProduct([FromForm] ProductDTO product, [FromQuery] int id)
        {
            try
            {
                string imagePath = string.Empty;

                if (product.Image != null && product.Image.Length > 0)
                {
                    imagePath = await CreatedImagePathAsync(product);
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
            catch (Exception ex)
            {
                Console.WriteLine($"UPDATE ERROR: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("AddProduct-with-image")]
        public async Task<IActionResult> AddProduct([FromForm] ProductDTO product)
        {
            try
            {
                string imagePath = string.Empty;
                if (product.Image != null && product.Image.Length > 0)
                {
                    imagePath = await CreatedImagePathAsync(product);
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
                return Ok(new { message = "Product added successfully", path = imagePath });
            }
            catch (Exception ex)
            {
                // This ensures you see the REAL error in Render Logs
                Console.WriteLine($"ADD PRODUCT ERROR: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<string> CreatedImagePathAsync(ProductDTO product)
        {
            // Use Path.Combine for Linux/Windows compatibility
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(product.Image.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // SAVE ASYNC: Critical for Render's stability
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await product.Image.CopyToAsync(fileStream);
            }

            return $"/images/{uniqueFileName}";
        }
    }
}