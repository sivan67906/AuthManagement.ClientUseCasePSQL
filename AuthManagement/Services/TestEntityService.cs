using AuthManagement.Models;

namespace AuthManagement.Services
{
    /// <summary>
    /// Mock service for testing RBAC permissions
    /// Uses in-memory storage to simulate database operations
    /// </summary>
    public class TestEntityService
    {
        private List<TestProductDto> _products = new();
        private List<TestCategoryDto> _categories = new();

        public TestEntityService()
        {
            // Initialize with sample data
            InitializeSampleData();
        }

        private void InitializeSampleData()
        {
            // Sample Products
            _products = new List<TestProductDto>
            {
                new TestProductDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Laptop Pro 15",
                    Description = "High-performance laptop for professionals",
                    Price = 1299.99m,
                    Stock = 50,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-30)
                },
                new TestProductDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Wireless Mouse",
                    Description = "Ergonomic wireless mouse with precision tracking",
                    Price = 29.99m,
                    Stock = 200,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-25)
                },
                new TestProductDto
                {
                    Id = Guid.NewGuid(),
                    Name = "USB-C Hub",
                    Description = "7-in-1 USB-C hub with multiple ports",
                    Price = 49.99m,
                    Stock = 150,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-20)
                },
                new TestProductDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Mechanical Keyboard",
                    Description = "RGB backlit mechanical keyboard with blue switches",
                    Price = 89.99m,
                    Stock = 75,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-15)
                },
                new TestProductDto
                {
                    Id = Guid.NewGuid(),
                    Name = "4K Monitor 27\"",
                    Description = "Ultra HD 4K monitor with HDR support",
                    Price = 399.99m,
                    Stock = 30,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-10)
                }
            };

            // Sample Categories
            _categories = new List<TestCategoryDto>
            {
                new TestCategoryDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Electronics",
                    Description = "Electronic devices and accessories",
                    Icon = "ti tabler-device-laptop",
                    DisplayOrder = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-60)
                },
                new TestCategoryDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Accessories",
                    Description = "Computer and mobile accessories",
                    Icon = "ti tabler-device-usb",
                    DisplayOrder = 2,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-55)
                },
                new TestCategoryDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Office Supplies",
                    Description = "Office and stationery items",
                    Icon = "ti tabler-briefcase",
                    DisplayOrder = 3,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-50)
                },
                new TestCategoryDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Software",
                    Description = "Software licenses and subscriptions",
                    Icon = "ti tabler-code",
                    DisplayOrder = 4,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-45)
                }
            };
        }

        // ============ PRODUCT OPERATIONS ============

        public Task<ApiResponse<List<TestProductDto>>> GetAllProductsAsync()
        {
            try
            {
                // Simulate API delay
                System.Threading.Thread.Sleep(300);
                return Task.FromResult(new ApiResponse<List<TestProductDto>>
                {
                    Success = true,
                    Data = _products
                    .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).ToList(),
                    Message = "Products retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResponse<List<TestProductDto>>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        public Task<ApiResponse<TestProductDto>> GetProductByIdAsync(Guid id)
        {
            try
            {
                var product = _products.FirstOrDefault(p => p.Id == id);
                if (product == null)
                {
                    return Task.FromResult(new ApiResponse<TestProductDto>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                return Task.FromResult(new ApiResponse<TestProductDto>
                {
                    Success = true,
                    Data = product,
                    Message = "Product retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResponse<TestProductDto>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        public Task<ApiResponse<TestProductDto>> CreateProductAsync(CreateTestProductRequest request)
        {
            try
            {
                // Simulate API delay
                System.Threading.Thread.Sleep(500);

                // Check for duplicate name
                if (_products.Any(p => p.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    return Task.FromResult(new ApiResponse<TestProductDto>
                    {
                        Success = false,
                        Message = "A product with this name already exists"
                    });
                }

                var newProduct = new TestProductDto
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Stock = request.Stock,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.Now
                };

                _products.Add(newProduct);

                return Task.FromResult(new ApiResponse<TestProductDto>
                {
                    Success = true,
                    Data = newProduct,
                    Message = "Product created successfully"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResponse<TestProductDto>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        public Task<ApiResponse<TestProductDto>> UpdateProductAsync(Guid id, UpdateTestProductRequest request)
        {
            try
            {
                // Simulate API delay
                System.Threading.Thread.Sleep(500);

                var product = _products.FirstOrDefault(p => p.Id == id);
                if (product == null)
                {
                    return Task.FromResult(new ApiResponse<TestProductDto>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                // Check for duplicate name (excluding current product)
                if (_products.Any(p => p.Id != id && p.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    return Task.FromResult(new ApiResponse<TestProductDto>
                    {
                        Success = false,
                        Message = "A product with this name already exists"
                    });
                }

                product.Name = request.Name;
                product.Description = request.Description;
                product.Price = request.Price;
                product.Stock = request.Stock;
                product.IsActive = request.IsActive;
                product.UpdatedAt = DateTime.Now;

                return Task.FromResult(new ApiResponse<TestProductDto>
                {
                    Success = true,
                    Data = product,
                    Message = "Product updated successfully"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResponse<TestProductDto>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        public Task<ApiResponse<bool>> DeleteProductAsync(Guid id)
        {
            try
            {
                // Simulate API delay
                System.Threading.Thread.Sleep(500);

                var product = _products.FirstOrDefault(p => p.Id == id);
                if (product == null)
                {
                    return Task.FromResult(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                _products.Remove(product);

                return Task.FromResult(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Product deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        // ============ CATEGORY OPERATIONS ============

        public Task<ApiResponse<List<TestCategoryDto>>> GetAllCategoriesAsync()
        {
            try
            {
                // Simulate API delay
                System.Threading.Thread.Sleep(300);
                return Task.FromResult(new ApiResponse<List<TestCategoryDto>>
                {
                    Success = true,
                    Data = _categories
                    .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                    .ToList(),
                    Message = "Categories retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResponse<List<TestCategoryDto>>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        public Task<ApiResponse<TestCategoryDto>> GetCategoryByIdAsync(Guid id)
        {
            try
            {
                var category = _categories.FirstOrDefault(c => c.Id == id);
                if (category == null)
                {
                    return Task.FromResult(new ApiResponse<TestCategoryDto>
                    {
                        Success = false,
                        Message = "Category not found"
                    });
                }

                return Task.FromResult(new ApiResponse<TestCategoryDto>
                {
                    Success = true,
                    Data = category,
                    Message = "Category retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResponse<TestCategoryDto>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        public Task<ApiResponse<TestCategoryDto>> CreateCategoryAsync(CreateTestCategoryRequest request)
        {
            try
            {
                // Simulate API delay
                System.Threading.Thread.Sleep(500);

                // Check for duplicate name
                if (_categories.Any(c => c.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    return Task.FromResult(new ApiResponse<TestCategoryDto>
                    {
                        Success = false,
                        Message = "A category with this name already exists"
                    });
                }

                var newCategory = new TestCategoryDto
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Description = request.Description,
                    Icon = request.Icon,
                    DisplayOrder = request.DisplayOrder,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.Now
                };

                _categories.Add(newCategory);

                return Task.FromResult(new ApiResponse<TestCategoryDto>
                {
                    Success = true,
                    Data = newCategory,
                    Message = "Category created successfully"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResponse<TestCategoryDto>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        public Task<ApiResponse<TestCategoryDto>> UpdateCategoryAsync(Guid id, UpdateTestCategoryRequest request)
        {
            try
            {
                // Simulate API delay
                System.Threading.Thread.Sleep(500);

                var category = _categories.FirstOrDefault(c => c.Id == id);
                if (category == null)
                {
                    return Task.FromResult(new ApiResponse<TestCategoryDto>
                    {
                        Success = false,
                        Message = "Category not found"
                    });
                }

                // Check for duplicate name (excluding current category)
                if (_categories.Any(c => c.Id != id && c.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    return Task.FromResult(new ApiResponse<TestCategoryDto>
                    {
                        Success = false,
                        Message = "A category with this name already exists"
                    });
                }

                category.Name = request.Name;
                category.Description = request.Description;
                category.Icon = request.Icon;
                category.DisplayOrder = request.DisplayOrder;
                category.IsActive = request.IsActive;
                category.UpdatedAt = DateTime.Now;

                return Task.FromResult(new ApiResponse<TestCategoryDto>
                {
                    Success = true,
                    Data = category,
                    Message = "Category updated successfully"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResponse<TestCategoryDto>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        public Task<ApiResponse<bool>> DeleteCategoryAsync(Guid id)
        {
            try
            {
                // Simulate API delay
                System.Threading.Thread.Sleep(500);

                var category = _categories.FirstOrDefault(c => c.Id == id);
                if (category == null)
                {
                    return Task.FromResult(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Category not found"
                    });
                }

                _categories.Remove(category);

                return Task.FromResult(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Category deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }
    }
}
