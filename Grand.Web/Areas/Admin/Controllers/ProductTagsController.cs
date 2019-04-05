﻿using Grand.Core.Domain.Seo;
using Grand.Framework.Extensions;
using Grand.Framework.Kendoui;
using Grand.Framework.Mvc;
using Grand.Framework.Security.Authorization;
using Grand.Services.Catalog;
using Grand.Services.Localization;
using Grand.Services.Security;
using Grand.Services.Seo;
using Grand.Web.Areas.Admin.Extensions;
using Grand.Web.Areas.Admin.Models.Catalog;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Areas.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.ProductTags)]
    public partial class ProductTagsController : BaseAdminController
    {
        private readonly IProductTagService _productTagService;
        private readonly IProductService _productService;
        private readonly ILanguageService _languageService;
        private readonly SeoSettings _seoSettings;
        public ProductTagsController(IProductTagService productTagService, IProductService productService, ILanguageService languageService, SeoSettings seoSettings)
        {
            this._productTagService = productTagService;
            this._productService = productService;
            this._languageService = languageService;
            this._seoSettings = seoSettings;
        }

        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List() => View();

        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var tags = (await _productTagService.GetAllProductTags())
                //order by product count
                .OrderByDescending(x => _productTagService.GetProductCount(x.Id, ""))
                .Select(x => new ProductTagModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    ProductCount = _productTagService.GetProductCount(x.Id, "")
                })
                .ToList();

            var gridModel = new DataSourceResult
            {
                Data = tags.PagedForCommand(command),
                Total = tags.Count
            };

            return Json(gridModel);
        }
        [HttpPost]
        public async Task<IActionResult> Products(string tagId, DataSourceRequest command)
        {
            var tag = await _productTagService.GetProductTagById(tagId);

            var products = (await _productService.SearchProducts(pageIndex: command.Page - 1, pageSize: command.PageSize, productTag: tag.Name, orderBy: Core.Domain.Catalog.ProductSortingEnum.NameAsc)).products;
            var gridModel = new DataSourceResult
            {
                Data = products.Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name,
                }),
                Total = products.TotalCount
            };

            return Json(gridModel);
        }

        //edit
        public async Task<IActionResult> Edit(string id)
        {
            var productTag = await _productTagService.GetProductTagById(id);
            if (productTag == null)
                //No product tag found with the specified id
                return RedirectToAction("List");

            var model = new ProductTagModel
            {
                Id = productTag.Id,
                Name = productTag.Name,
                ProductCount = _productTagService.GetProductCount(productTag.Id, "")
            };
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = productTag.GetLocalized(x => x.Name, languageId, false, false);
            });

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ProductTagModel model)
        {
            var productTag = await _productTagService.GetProductTagById(model.Id);
            if (productTag == null)
                //No product tag found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                productTag.Name = model.Name;
                productTag.Locales = model.Locales.ToLocalizedProperty();
                productTag.SeName = SeoExtensions.GetSeName(productTag.Name, _seoSettings);
                await _productTagService.UpdateProductTag(productTag);
                ViewBag.RefreshPage = true;
                return View(model);
            }
            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var tag = await _productTagService.GetProductTagById(id);
            if (tag == null)
                throw new ArgumentException("No product tag found with the specified id");
            if (ModelState.IsValid)
            {
                await _productTagService.DeleteProductTag(tag);
                return new NullJsonResult();
            }
            return ErrorForKendoGridJson(ModelState);
        }
    }
}
