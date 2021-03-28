﻿using Domain.Interfaces.InterfaceCompraUsuario;
using Entities.Entities;
using Entities.Entities.Enums;
using Infrastructure.Configuration;
using Infrastructure.Repository.Generics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using System.Linq;

namespace Infrastructure.Repository.Repositories
{
    public class RepositoryCompraUsuario : RepositoryGenerics<CompraUsuario>, ICompraUsuario
    {

        private readonly DbContextOptions<ContextBase> _optionsbuilder;

        public RepositoryCompraUsuario()
        {
            _optionsbuilder = new DbContextOptions<ContextBase>();
        }

        public async Task<bool> ConfirmaCompraCarrinhoUsuario(string userId)
        {
            try
            {
                using (var banco = new ContextBase(_optionsbuilder))
                {
                    var compraUsuario = new CompraUsuario();
                    compraUsuario.ListaProdutos = new List<Produto>();

                    var produtosCarrinhoUsuario = await (from p in banco.Produto
                                                         join c in banco.CompraUsuario on p.Id equals c.ProdutoID
                                                         where c.UsuarioID.Equals(userId) && c.EnumEstadoCompra == EnumEstadoCompra.Produto_Carrinho
                                                         select c).AsNoTracking().ToListAsync();

                    produtosCarrinhoUsuario.ForEach(p =>
                    {
                        p.EnumEstadoCompra = EnumEstadoCompra.Produto_Comprado;
                    });

                    banco.UpdateRange(produtosCarrinhoUsuario);
                    await banco.SaveChangesAsync();

                    return true;
                }
            }
            catch (Exception erro)
            {
                return false;
            }

        }

        public async Task<CompraUsuario> ProdutosCompradosPorEstado(string userId, EnumEstadoCompra estado)
        {
            using (var banco = new ContextBase(_optionsbuilder))
            {
                var compraUsuario = new CompraUsuario();
                compraUsuario.ListaProdutos = new List<Produto>();

                var produtosCarrinhoUsuario = await (from p in banco.Produto
                                                     join c in banco.CompraUsuario on p.Id equals c.ProdutoID
                                                     where c.UsuarioID.Equals(userId) && c.EnumEstadoCompra == estado
                                                     select new Produto
                                                     {
                                                         Id = p.Id,
                                                         Nome = p.Nome,
                                                         Descricao = p.Descricao,
                                                         Observacao = p.Observacao,
                                                         Valor = p.Valor,
                                                         QtdCompra = c.Quantidade,
                                                         IdProdutoCarrinho = c.CompraUsuarioID,
                                                         Url = p.Url,
                                                     }).AsNoTracking().ToListAsync();


                compraUsuario.ListaProdutos = produtosCarrinhoUsuario;
                compraUsuario.Usuario = await banco.ApplicationUser.FirstOrDefaultAsync(u => u.Id.Equals(userId));
                compraUsuario.QuantidadeProdutos = produtosCarrinhoUsuario.Count();
                compraUsuario.EnderecoCompleto = string.Concat(compraUsuario.Usuario.Endereco, " - ", compraUsuario.Usuario.ComplementoEndereco, " - CEP: ", compraUsuario.Usuario.CEP);
                compraUsuario.ValorTotal = produtosCarrinhoUsuario.Sum(v => v.Valor);
                compraUsuario.EnumEstadoCompra = estado;
                return compraUsuario;


            }
        }

        public async Task<int> QuantidadeProdutoCarrinhoUsuario(string userId)
        {
            using (var banco = new ContextBase(_optionsbuilder))
            {
                return await banco.CompraUsuario.CountAsync(c => c.UsuarioID.Equals(userId) && c.EnumEstadoCompra == EnumEstadoCompra.Produto_Carrinho);
            }
        }
    }
}
