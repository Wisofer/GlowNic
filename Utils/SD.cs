namespace GlowNic.Utils;

public static class SD
{
    // Roles de usuario
    public const string RolAdministrador = "Administrador";
    public const string RolNormal = "Normal";
    public const string RolCaja = "Caja";
    public const string RolMecanico = "Mecánico";

    // Estados de Factura
    public const string EstadoFacturaPendiente = "Pendiente";
    public const string EstadoFacturaPagada = "Pagada";
    public const string EstadoFacturaCancelada = "Cancelada";

    // Estados de Orden de Trabajo (Simplificados)
    public const string EstadoOTRecibida = "Recibida";
    public const string EstadoOTEnProceso = "En Proceso";
    public const string EstadoOTTerminada = "Terminada";
    public const string EstadoOTEntregada = "Entregada";
    public const string EstadoOTCancelada = "Cancelada";
    
    // Estados antiguos (mantener para compatibilidad con datos existentes)
    public const string EstadoOTEnDiagnostico = "En diagnóstico";
    public const string EstadoOTEnReparacion = "En reparación";
    public const string EstadoOTEsperandoRepuestos = "Esperando repuestos";

    // Tipos de Pago
    public const string TipoPagoFisico = "Fisico";
    public const string TipoPagoElectronico = "Electronico";
    public const string TipoPagoMixto = "Mixto";

    // Monedas
    public const string MonedaCordoba = "C$";
    public const string MonedaDolar = "$";
    public const string MonedaAmbos = "Ambos";

    // Tipo de Cambio
    public const decimal TipoCambioDolar = 36.80m; // C$36.80 = $1

    // Bancos
    public const string BancoBanpro = "Banpro";
    public const string BancoLafise = "Lafise";
    public const string BancoBAC = "BAC";
    public const string BancoFicohsa = "Ficohsa";
    public const string BancoBDF = "BDF";

    // Tipos de Cuenta
    public const string TipoCuentaDolar = "Cuenta $";
    public const string TipoCuentaCordoba = "Cuenta C$";
    public const string TipoCuentaBilletera = "Billetera movil";

    // Categorías de Servicios del Taller - ELIMINADAS (ya no se usan)

    // Categorías de Productos/Repuestos
    public const string CategoriaProductoAceites = "Aceites";
    public const string CategoriaProductoFiltros = "Filtros";
    public const string CategoriaProductoFrenos = "Frenos";
    public const string CategoriaProductoLlantas = "Llantas";
    public const string CategoriaProductoBaterias = "Baterías";
    public const string CategoriaProductoBujias = "Bujías";
    public const string CategoriaProductoCadenas = "Cadenas";
    public const string CategoriaProductoRepuestosVarios = "Repuestos varios";

    // Tipos de Movimiento de Inventario
    public const string TipoMovimientoEntrada = "Entrada";
    public const string TipoMovimientoSalida = "Salida";

    // Subtipos de Movimiento de Inventario
    public const string SubtipoMovimientoCompra = "Compra";
    public const string SubtipoMovimientoVenta = "Venta";
    public const string SubtipoMovimientoDevolucion = "Devolución";
    public const string SubtipoMovimientoAjuste = "Ajuste";
    public const string SubtipoMovimientoDano = "Daño";
    public const string SubtipoMovimientoTransferencia = "Transferencia";

    // Tipos de Egreso
    public const string TipoEgresoCompraRepuestos = "Compra repuestos";
    public const string TipoEgresoGastoOperativo = "Gasto operativo";
    public const string TipoEgresoPagoProveedor = "Pago proveedor";
    public const string TipoEgresoOtro = "Otro";
    public const string TipoEgresoOperativo = "Operativo";

    // Categorías de Egreso
    public const string CategoriaEgresoDevolucionCliente = "Devolución a cliente";

    // Estados de Devolución
    public const string EstadoDevolucionPendiente = "Pendiente";
    public const string EstadoDevolucionAprobada = "Aprobada";
    public const string EstadoDevolucionRechazada = "Rechazada";

    // Métodos de Pago para Egresos
    public const string MetodoPagoEfectivo = "Efectivo";
    public const string MetodoPagoTransferencia = "Transferencia";
    public const string MetodoPagoCheque = "Cheque";

    // Estados de Caja
    public const string EstadoCajaAbierta = "Abierta";
    public const string EstadoCajaCerrada = "Cerrada";

    // Tipos de Ubicación
    public const string TipoUbicacionAlmacen = "Almacen";
    public const string TipoUbicacionTaller = "Taller";
    public const string TipoUbicacionMostrador = "Mostrador";
    public const string TipoUbicacionCampo = "Campo"; // Mantener para compatibilidad
    public const string TipoUbicacionReparacion = "Reparacion"; // Mantener para compatibilidad

    // Tipos de Factura
    public const string TipoFacturaOrdenTrabajo = "OrdenTrabajo";
    public const string TipoFacturaVentaDirecta = "VentaDirecta";

    // Estados de Equipo (Productos/Repuestos)
    public const string EstadoEquipoDisponible = "Disponible";
    public const string EstadoEquipoEnUso = "En uso";
    public const string EstadoEquipoDanado = "Dañado";
    public const string EstadoEquipoEnReparacion = "En reparación";
    public const string EstadoEquipoRetirado = "Retirado";

    // Estados de Asignación de Equipo
    public const string EstadoAsignacionActiva = "Activa";
    public const string EstadoAsignacionDevuelta = "Devuelta";

    // Series de Facturación
    public const string SerieFacturaA = "A";
    public const string SerieFacturaB = "B";
    public const string SerieFacturaC = "C";

    // Tipos de Mantenimiento/Reparación
    public const string TipoMantenimientoPreventivo = "Preventivo";
    public const string TipoMantenimientoCorrectivo = "Correctivo";

    // Estados de Mantenimiento/Reparación
    public const string EstadoMantenimientoProgramado = "Programado";
    public const string EstadoMantenimientoEnProceso = "En proceso";
    public const string EstadoMantenimientoCompletado = "Completado";
    public const string EstadoMantenimientoCancelado = "Cancelado";
}
