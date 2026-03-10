# Flujos de negocio

## Flujo de ventas

1. Vendedor inicia sesión
2. Descarga su ruta y clientes asignados
3. Registra venta con productos y montos
4. Sistema guarda localmente si no hay red
5. Al recuperar conectividad, sincroniza con backend

## Flujo de cobros con geolocalización

1. Cobrador abre lista de cuentas por cobrar
2. Selecciona cliente y registra pago
3. App solicita ubicación GPS en el momento del cobro
4. Guarda evidencia (latitud, longitud, hora, usuario)
5. Sincroniza datos al backend y actualiza cartera

## Flujo de dashboard

1. Supervisor/Admin consulta indicadores
2. Backend consolida ventas, cobros y desempeño por zona
3. Frontend muestra KPIs y tendencias diarias/semanales

## KPIs recomendados

- Venta diaria por vendedor
- Cobranza diaria por cobrador
- % cartera vencida
- Cobros con geolocalización válida
- Cumplimiento de ruta
