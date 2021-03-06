﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using NuGet.Versioning;
using NuGetGallery.Areas.Admin.Services;
using NuGetGallery.Areas.Admin.ViewModels;

namespace NuGetGallery.Areas.Admin.Controllers
{
    public class ValidationController : AdminControllerBase
    {
        private readonly ValidationAdminService _validationAdminService;

        public ValidationController(ValidationAdminService validationAdminService)
        {
            _validationAdminService = validationAdminService ?? throw new ArgumentNullException(nameof(validationAdminService));
        }

        [HttpGet]
        public virtual ActionResult Index()
        {
            return View(nameof(Index), new ValidationPageViewModel());
        }

        [HttpGet]
        public virtual ActionResult Search(string q)
        {
            var packageValidationSets = _validationAdminService.Search(q ?? string.Empty);

            // Sort by lexigraphically package ID then put newer stuff on top.
            var groups = packageValidationSets
                .OrderBy(x => x.PackageId)
                .ThenByDescending(x => NuGetVersion.Parse(x.PackageNormalizedVersion))
                .ThenByDescending(x => x.PackageKey)
                .ThenByDescending(x => x.Created)
                .ThenByDescending(x => x.Key)
                .GroupBy(x => x.PackageKey);

            var validatedPackages = new List<ValidatedPackageViewModel>();
            foreach (var group in groups)
            {
                foreach (var set in group)
                {
                    // Put completed validations first then put new validations on top.
                    set.PackageValidations = set.PackageValidations
                        .OrderByDescending(x => x.ValidationStatus)
                        .ThenByDescending(x => x.ValidationStatusTimestamp)
                        .ToList();
                }

                var deletedStatus = _validationAdminService.GetPackageDeletedStatus(group.Key);
                var validatedPackage = new ValidatedPackageViewModel(group.ToList(), deletedStatus);
                validatedPackages.Add(validatedPackage);
            }

            return View(nameof(Index), new ValidationPageViewModel(q, validatedPackages));
        }
    }
}