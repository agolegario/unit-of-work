using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Linq.Expressions;
using AutoMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleInjector;
using SimpleInjector.Extensions.LifetimeScoping;

namespace UnitOfWork
{

    #region  Tests

    [TestClass]
    public class UnitTestUnitOfWork
    {
        private Container _container;

        [TestInitialize]
        public void Inicialize()
        {
            AutoMapperConfig.RegisterMappings();
            _container = new Container();
            _container.Options.DefaultScopedLifestyle = new LifetimeScopeLifestyle();
            // _container.Register<IUnitOfWork, MyContext>(Lifestyle.Scoped); // WEB
            _container.Register<IUnitOfWork, MyContext>(Lifestyle.Singleton); // APP
            _container.Register<IPessoaRepository, PessoaRepository>();
            _container.Register<IPessoaService, PessoaService>();
        }

        [TestMethod]
        public void Test_Injector_Singleton()
        {
                var contexto2 = _container.GetInstance<IUnitOfWork>();
                var contexto1 = _container.GetInstance<IUnitOfWork>();
                Assert.AreSame(contexto1, contexto2);
        }

        [TestMethod]
        public void Test_Injector_Scope()
        {
            using (_container.BeginLifetimeScope())
            {
                var contexto2 = _container.GetInstance<IUnitOfWork>();
                var contexto1 = _container.GetInstance<IUnitOfWork>();
                Assert.AreSame(contexto1, contexto2);
            }
        }

        [TestMethod]
        public void Test_Crud_App_To_Domain()
        {
            var service = _container.GetInstance<IPessoaService>();
            var pessoaModel = new PessoaModel { Nome = "TESTE-CRUD" };
            service.Add(pessoaModel);

            pessoaModel = null;
            pessoaModel = service.Find(s => s.Nome.Equals("TESTE-CRUD")).FirstOrDefault();

            if (pessoaModel == null) throw new ArgumentNullException("Valor nulo !");

            pessoaModel.Nome = "TESTE";
            service.Update(pessoaModel);

            pessoaModel = null;
            pessoaModel = service.Find(s => s.Nome.Equals("TESTE")).FirstOrDefault();

            if (pessoaModel == null) throw new ArgumentNullException("Valor nulo !");

            pessoaModel.Nome = "TESTE-CRUD";
            service.Update(pessoaModel);

            pessoaModel = null;
            pessoaModel = service.Find(s => s.Nome.Equals("TESTE-CRUD")).FirstOrDefault();
            if (pessoaModel == null) throw new ArgumentNullException("Valor nulo");
            service.Delete(pessoaModel);

            pessoaModel = null;
            pessoaModel = service.Find(s => s.Nome.Equals("TESTE-CRUD")).FirstOrDefault();
            Assert.IsNull(pessoaModel);

            service.Dispose();
        }
    }

    #endregion

    #region  Application

    public class PessoaModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
    }

    public interface IPessoaService
    {
        void Add(PessoaModel pessoa);
        void Update(PessoaModel pessoa);
        void Delete(PessoaModel pessoa);
        PessoaModel GetById(int id);
        IEnumerable<PessoaModel> GetAll();
        IEnumerable<PessoaModel> Find(Expression<Func<Pessoa, bool>> predicate);
        void Dispose();
    }

    public class PessoaService : IPessoaService
    {
        private readonly IPessoaRepository _pessoaRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PessoaService(IUnitOfWork unitOfWork, IPessoaRepository pessoaRepository)
        {
            _unitOfWork = unitOfWork;
            _pessoaRepository = pessoaRepository;
        }

        public void Add(PessoaModel pessoa)
        {
            var produto = Mapper.Map<PessoaModel, Pessoa>(pessoa);
            _pessoaRepository.Add(produto);
            _unitOfWork.Commit();
        }

        public void Update(PessoaModel pessoa)
        {
            var produto = Mapper.Map<PessoaModel, Pessoa>(pessoa);
            _pessoaRepository.Update(produto);
            _unitOfWork.Commit();
        }

        public void Delete(PessoaModel pessoa)
        {
            var produto = Mapper.Map<PessoaModel, Pessoa>(pessoa);
            _pessoaRepository.Delete(produto);
            _unitOfWork.Commit();
        }

        public PessoaModel GetById(int id)
        {
            return Mapper.Map<Pessoa, PessoaModel>(_pessoaRepository.GetById(id));
        }

        public IEnumerable<PessoaModel> GetAll()
        {
            return Mapper.Map<IEnumerable<Pessoa>, IEnumerable<PessoaModel>>(_pessoaRepository.GetAll());
        }

        public IEnumerable<PessoaModel> Find(Expression<Func<Pessoa, bool>> predicate)
        {
            return Mapper.Map<IEnumerable<Pessoa>, IEnumerable<PessoaModel>>(_pessoaRepository.Find(predicate));
        }

        public void Dispose()
        {
            _pessoaRepository.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class AutoMapperConfig
    {
        public static void RegisterMappings()
        {
            Mapper.Initialize(x =>
            {
                x.AddProfile<DomainToApplicationProfile>();
                x.AddProfile<ApplicationToDomainProfile>();
            });
        }
    }

    public class ApplicationToDomainProfile : Profile
    {
        public override string ProfileName => "ApplicationToDomainProfile";

        protected override void Configure()
        {
            Mapper.CreateMap<PessoaModel, Pessoa>();
        }
    }

    public class DomainToApplicationProfile : Profile
    {
        public override string ProfileName => "DomainToApplicationProfile";

        protected override void Configure()
        {
            Mapper.CreateMap<Pessoa, PessoaModel>();
        }
    }

    #endregion

    #region  Domain

    public class Pessoa
    {
        public int Id { get; set; }
        public string Nome { get; set; }
    }

    #endregion

    #region  Infra

    public interface IPessoaRepository : IDisposable
    {
        void Add(Pessoa pessoa);
        void Update(Pessoa pessoa);
        void Delete(Pessoa pessoa);
        Pessoa GetById(int id);
        IEnumerable<Pessoa> GetAll();
        IEnumerable<Pessoa> Find(Expression<Func<Pessoa, bool>> predicate);
    }

    public class PessoaRepository : IPessoaRepository
    {
        private readonly IUnitOfWork _context;

        public PessoaRepository(IUnitOfWork context)
        {
            _context = context;
        }

        public void Add(Pessoa pessoa)
        {
            _context.Pessoas.Add(pessoa);
        }

        public void Update(Pessoa pessoa)
        {
            _context.Pessoas.AddOrUpdate(pessoa);
        }

        public void Delete(Pessoa pessoa)
        {
            var pessoaTemp = _context.Pessoas.Find(pessoa.Id);
            _context.Pessoas.Remove(pessoaTemp);
        }

        public Pessoa GetById(int id)
        {
            return _context.Pessoas.FirstOrDefault(s => s.Id == id);
        }

        public IEnumerable<Pessoa> GetAll()
        {
            return _context.Pessoas.ToList();
        }

        public IEnumerable<Pessoa> Find(Expression<Func<Pessoa, bool>> predicate)
        {
            return _context.Pessoas.Where(predicate).ToList();
        }

        public void Dispose()
        {
            _context.Disponse();
            GC.SuppressFinalize(this);
        }
    }

    public interface IUnitOfWork
    {
        IDbSet<Pessoa> Pessoas { get; set; }
        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        //DbEntityEntry Entry(object entity);
        void Disponse();
        int Commit();
    }

    public class MyContext : DbContext, IUnitOfWork
    {
        public MyContext() : base("Conn")
        {
            Database.SetInitializer<MyContext>(null);
        }

        public IDbSet<Pessoa> Pessoas { get; set; }

        public int Commit()
        {
            return SaveChanges();
        }

        public void Disponse()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Configurations.Add(new PessoaMap());
            base.OnModelCreating(modelBuilder);
        }
    }

    public class PessoaMap : EntityTypeConfiguration<Pessoa>
    {
        public PessoaMap()
        {
            HasKey(s => s.Id);
            Property(s => s.Nome).HasMaxLength(100).HasColumnType("varchar");
        }
    }

    #endregion
}