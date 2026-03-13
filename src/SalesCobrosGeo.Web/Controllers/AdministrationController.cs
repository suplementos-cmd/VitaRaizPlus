using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Administration;

namespace SalesCobrosGeo.Web.Controllers;

public sealed class AdministrationController : Controller
{
    public IActionResult Users()
    {
        var roles = new[]
        {
            new RoleProfileCard(
                "VENDEDOR",
                "Vendedor",
                "Registra ventas propias y consulta solo su cartera comercial.",
                new RoleTheme("Comercial", "#2c74d8", "#7bb3ff", "#edf5ff"),
                ["Inicio", "Ventas propias", "Detalle de venta", "Catalogos basicos"],
                [
                    new RolePermissionRow("Ventas", "Ver/crear/editar propias", "Cliente, producto, zona, fotos, coordenadas, forma de pago"),
                    new RolePermissionRow("Cobros", "Sin acceso", "No aplica"),
                    new RolePermissionRow("Dashboard", "Resumen personal", "Totales de ventas propias")
                ]),
            new RoleProfileCard(
                "SUPERVISOR",
                "Supervisor de ventas",
                "Controla ventas del equipo y corrige informacion comercial.",
                new RoleTheme("Supervision", "#1f7a63", "#74d7b6", "#ecfbf5"),
                ["Inicio", "Ventas equipo", "Dashboard comercial", "Mantenimiento catalogos"],
                [
                    new RolePermissionRow("Ventas", "Ver/editar equipo", "Todos los campos de venta, comision, cobrador asignado"),
                    new RolePermissionRow("Cobros", "Solo consulta", "Estado de cobranza y restante"),
                    new RolePermissionRow("Dashboard", "Equipo comercial", "Totales por vendedora y zona")
                ]),
            new RoleProfileCard(
                "COBRADOR",
                "Cobrador",
                "Opera ruta diaria, registra cobros y consulta cartera asignada.",
                new RoleTheme("Cobranza", "#a44b24", "#f2a86c", "#fff4ea"),
                ["Inicio", "Cobros", "Detalle de cobro", "Mapa de ruta"],
                [
                    new RolePermissionRow("Cobros", "Ver/registrar propios", "Importe cobrado, observacion, coordenadas, historial"),
                    new RolePermissionRow("Ventas", "Consulta limitada", "Cliente, zona, dia de cobro, fotos base"),
                    new RolePermissionRow("Dashboard", "Ruta personal", "Pendiente, abonado, atrasado")
                ]),
            new RoleProfileCard(
                "COBRADOR_SUP",
                "Supervisor de cobranza",
                "Monitorea cobradores, cartera vencida y zonas con atraso.",
                new RoleTheme("Control", "#7a2fd0", "#b98dff", "#f4efff"),
                ["Cobros globales", "Dashboard de cobranza", "Usuarios de cobranza"],
                [
                    new RolePermissionRow("Cobros", "Ver todo / reasignar", "Estados, cobrador, zona, historial completo"),
                    new RolePermissionRow("Ventas", "Consulta", "Campos comerciales y saldo"),
                    new RolePermissionRow("Dashboard", "Cobros por zona y cobrador", "Totales, atrasos, cuentas por dia")
                ]),
            new RoleProfileCard(
                "ADMIN",
                "Administrador",
                "Gestion total del sistema, usuarios, permisos, catalogos y KPIs.",
                new RoleTheme("Administracion", "#24334f", "#6da7ff", "#eef4ff"),
                ["Todas las vistas", "Mantenimiento", "Usuarios", "Dashboard general", "Permisos"],
                [
                    new RolePermissionRow("Usuarios", "Alta/edicion", "Nombre, correo, perfil, tema, estatus, zona"),
                    new RolePermissionRow("Permisos", "Total", "Vistas permitidas, campos visibles, acciones por rol"),
                    new RolePermissionRow("Dashboard", "Total", "Ventas, cobros, entradas, salidas, equipo, documentos")
                ])
        };

        var users = new[]
        {
            new AdminUserCard("Jake", "jake@vitaraiz.local", "VENDEDOR", "Heroes Chalco", "Activo", "Hoy 08:30"),
            new AdminUserCard("Lucia", "lucia@vitaraiz.local", "VENDEDOR", "Jardines", "Activo", "Hoy 09:10"),
            new AdminUserCard("Silvia", "silvia@vitaraiz.local", "COBRADOR", "Xico", "En ruta", "Hoy 07:45"),
            new AdminUserCard("Mario", "mario@vitaraiz.local", "COBRADOR_SUP", "Centro", "Activo", "Hoy 08:02"),
            new AdminUserCard("Supervisor general", "supervisor@vitaraiz.local", "SUPERVISOR", "Norte", "Activo", "Hoy 08:55"),
            new AdminUserCard("Administrador", "admin@vitaraiz.local", "ADMIN", "Global", "Activo", "Hoy 06:50")
        };

        return View(new AdministrationPageViewModel(roles, users));
    }
}
