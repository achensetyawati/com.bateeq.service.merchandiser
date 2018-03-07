﻿using Com.Bateeq.Service.Merchandiser.Lib.Helpers;
using Com.Bateeq.Service.Merchandiser.Lib.Interfaces;
using Com.Bateeq.Service.Merchandiser.Lib.Models;
using Com.Bateeq.Service.Merchandiser.Lib.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Com.Moonlay.NetCore.Lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Threading.Tasks;
using Com.Moonlay.NetCore.Lib.Service;
using Microsoft.EntityFrameworkCore;

namespace Com.Bateeq.Service.Merchandiser.Lib.Services
{
    public class RO_RetailService : BasicService<MerchandiserDbContext, RO_Retail>, IMap<RO_Retail, RO_RetailViewModel>
    {
        public RO_RetailService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override Tuple<List<RO_Retail>, int, Dictionary<string, string>, List<string>> ReadModel(int Page = 1, int Size = 25, string Order = "{}", List<string> Select = null, string Keyword = null, string Filter = "{}")
        {
            IQueryable<RO_Retail> Query = this.DbContext.RO_Retails;

            List<string> SearchAttributes = new List<string>()
                {
                    "Code"
                };
            Query = ConfigureSearch(Query, SearchAttributes, Keyword);

            Dictionary<string, object> FilterDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(Filter);
            Query = ConfigureFilter(Query, FilterDictionary);

            List<string> SelectedFields = new List<string>()
                {
                    "Id", "Code", "CostCalculationRetail", "Total"
                };
            Query = Query
                .Select(ro => new RO_Retail
                {
                    Id = ro.Id,
                    Code = ro.Code,
                    CostCalculationRetail = new CostCalculationRetail()
                    {
                        RO = ro.CostCalculationRetail.RO,
                        Article = ro.CostCalculationRetail.Article,
                        StyleId = ro.CostCalculationRetail.StyleId,
                        StyleName = ro.CostCalculationRetail.StyleName,
                        CounterId = ro.CostCalculationRetail.CounterId,
                        CounterName = ro.CostCalculationRetail.CounterName,
                    },
                    Total = ro.Total
                });

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            Query = ConfigureOrder(Query, OrderDictionary);

            Pageable<RO_Retail> pageable = new Pageable<RO_Retail>(Query, Page - 1, Size);
            List<RO_Retail> Data = pageable.Data.ToList<RO_Retail>();
            int TotalData = pageable.TotalCount;

            return Tuple.Create(Data, TotalData, OrderDictionary, SelectedFields);
        }

        public override async Task<int> CreateModel(RO_Retail Model)
        {
            CostCalculationRetail costCalculationRetail = Model.CostCalculationRetail;
            Model.CostCalculationRetail = null;

            int created = await this.CreateAsync(Model);

            CostCalculationRetailService costCalculationRetailService = this.ServiceProvider.GetService<CostCalculationRetailService>();
            costCalculationRetail.RO_RetailId = Model.Id;
            await costCalculationRetailService.UpdateModel(costCalculationRetail.Id, costCalculationRetail);

            return created;
        }

        public override void OnCreating(RO_Retail model)
        {
            do
            {
                model.Code = CodeGenerator.GenerateCode();
            }
            while (this.DbSet.Any(d => d.Code.Equals(model.Code)));

            if (model.RO_Retail_SizeBreakdowns.Count > 0)
            {
                RO_Retail_SizeBreakdownService RO_Retail_SizeBreakdownService = this.ServiceProvider.GetService<RO_Retail_SizeBreakdownService>();
                foreach (RO_Retail_SizeBreakdown RO_Retail_SizeBreakdown in model.RO_Retail_SizeBreakdowns)
                {
                    RO_Retail_SizeBreakdownService.Creating(RO_Retail_SizeBreakdown);
                }
            }

            base.OnCreating(model);
        }

        public override async Task<RO_Retail> ReadModelById(int id)
        {
            return await this.DbSet
                .Where(d => d.Id.Equals(id) && d._IsDeleted.Equals(false))
                .Include(d => d.RO_Retail_SizeBreakdowns)
                .Include(d => d.CostCalculationRetail)
                    .ThenInclude(ccr => ccr.CostCalculationRetail_Materials)
                .FirstOrDefaultAsync();
        }

        public override async Task<int> UpdateModel(int Id, RO_Retail Model)
        {
            CostCalculationRetail costCalculationRetail = Model.CostCalculationRetail;
            Model.CostCalculationRetail = null;

            int updated = await this.UpdateAsync(Id, Model);

            CostCalculationRetailService costCalculationRetailService = this.ServiceProvider.GetService<CostCalculationRetailService>();
            costCalculationRetail.RO_RetailId = Model.Id;
            await costCalculationRetailService.UpdateModel(costCalculationRetail.Id, costCalculationRetail);

            return updated;
        }

        public override void OnUpdating(int id, RO_Retail model)
        {
            RO_Retail_SizeBreakdownService RO_Retail_SizeBreakdownService = this.ServiceProvider.GetService<RO_Retail_SizeBreakdownService>();
            HashSet<int> RO_Retail_SizeBreakdowns = new HashSet<int>(RO_Retail_SizeBreakdownService.DbSet
                .Where(p => p.RO_RetailId.Equals(id))
                .Select(p => p.Id));

            foreach (int RO_Retail_SizeBreakdown in RO_Retail_SizeBreakdowns)
            {
                RO_Retail_SizeBreakdown childModel = model.RO_Retail_SizeBreakdowns.FirstOrDefault(prop => prop.Id.Equals(RO_Retail_SizeBreakdown));

                if (childModel == null)
                {
                    RO_Retail_SizeBreakdownService.Deleting(RO_Retail_SizeBreakdown);
                }
                else
                {
                    RO_Retail_SizeBreakdownService.Updating(RO_Retail_SizeBreakdown, childModel);
                }
            }

            foreach (RO_Retail_SizeBreakdown RO_Retail_SizeBreakdown in model.RO_Retail_SizeBreakdowns)
            {
                if (RO_Retail_SizeBreakdown.Id.Equals(0))
                {
                    RO_Retail_SizeBreakdownService.Creating(RO_Retail_SizeBreakdown);
                }
            }

            base.OnUpdating(id, model);
        }

        public override async Task<int> DeleteModel(int Id)
        {
            int deleted = await this.DeleteAsync(Id);

            CostCalculationRetailService costCalculationRetailService = this.ServiceProvider.GetService<CostCalculationRetailService>();
            CostCalculationRetail costCalculationRetail = costCalculationRetailService.DbSet
                .FirstOrDefault(p => p.RO_RetailId.Equals(Id));
            costCalculationRetail.RO_RetailId = null;

            await costCalculationRetailService.UpdateModel(costCalculationRetail.Id, costCalculationRetail);

            return deleted;
        }

        public override void OnDeleting(RO_Retail model)
        {
            RO_Retail_SizeBreakdownService RO_Retail_SizeBreakdownService = this.ServiceProvider.GetService<RO_Retail_SizeBreakdownService>();
            HashSet<int> RO_Retail_SizeBreakdowns = new HashSet<int>(RO_Retail_SizeBreakdownService.DbSet
                .Where(p => p.RO_RetailId.Equals(model.Id))
                .Select(p => p.Id));

            foreach (int RO_Retail_SizeBreakdown in RO_Retail_SizeBreakdowns)
            {
                RO_Retail_SizeBreakdownService.Deleting(RO_Retail_SizeBreakdown);
            }

            base.OnDeleting(model);
        }

        public RO_RetailViewModel MapToViewModel(RO_Retail model)
        {
            RO_RetailViewModel viewModel = new RO_RetailViewModel();
            PropertyCopier<RO_Retail, RO_RetailViewModel>.Copy(model, viewModel);

            CostCalculationRetailService costCalculationRetailService = this.ServiceProvider.GetService<CostCalculationRetailService>();
            viewModel.CostCalculationRetail = costCalculationRetailService.MapToViewModel(model.CostCalculationRetail);

            viewModel.Color = new ArticleColorViewModel()
            {
                _id = model.ColorId,
                name = model.ColorName
            };

            viewModel.RO_Retail_SizeBreakdowns = new List<RO_Retail_SizeBreakdownViewModel>();
            RO_Retail_SizeBreakdownService ro_RetailSizeBreakdownService = this.ServiceProvider.GetService<RO_Retail_SizeBreakdownService>();
            if (model.RO_Retail_SizeBreakdowns != null)
            {
                foreach (RO_Retail_SizeBreakdown ro_rsb in model.RO_Retail_SizeBreakdowns)
                {
                    RO_Retail_SizeBreakdownViewModel ro_rsbVM = ro_RetailSizeBreakdownService.MapToViewModel(ro_rsb);
                    viewModel.RO_Retail_SizeBreakdowns.Add(ro_rsbVM);
                }
            }

            if (model.SizeQuantityTotal != null)
            {
                viewModel.SizeQuantityTotal = JsonConvert.DeserializeObject<Dictionary<string, int>>(model.SizeQuantityTotal);
            }

            return viewModel;
        }

        public RO_Retail MapToModel(RO_RetailViewModel viewModel)
        {
            RO_Retail model = new RO_Retail();
            PropertyCopier<RO_RetailViewModel, RO_Retail>.Copy(viewModel, model);

            CostCalculationRetailService costCalculationRetailService = this.ServiceProvider.GetService<CostCalculationRetailService>();
            model.CostCalculationRetailId = viewModel.CostCalculationRetail.Id;
            model.CostCalculationRetail = costCalculationRetailService.MapToModel(viewModel.CostCalculationRetail);

            model.ColorId = viewModel.Color._id;
            model.ColorName = viewModel.Color.name;

            model.RO_Retail_SizeBreakdowns = new List<RO_Retail_SizeBreakdown>();
            RO_Retail_SizeBreakdownService RO_Retail_SizeBreakdownService = this.ServiceProvider.GetService<RO_Retail_SizeBreakdownService>();
            foreach (RO_Retail_SizeBreakdownViewModel ro_rsbVM in viewModel.RO_Retail_SizeBreakdowns)
            {
                RO_Retail_SizeBreakdown ro_rsb = RO_Retail_SizeBreakdownService.MapToModel(ro_rsbVM);
                model.RO_Retail_SizeBreakdowns.Add(ro_rsb);
            }
            model.SizeQuantityTotal = JsonConvert.SerializeObject(viewModel.SizeQuantityTotal);

            return model;
        }
    }
}
