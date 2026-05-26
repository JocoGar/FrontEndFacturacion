$Proyecto = "FrontendFacturacion"
$PublishDir = ".\publish-linux"
$Usuario = "jose"

$Nodos = @(
    "192.168.1.203",
    "192.168.1.204"
)

Write-Host "=========================================="
Write-Host "Publicando proyecto ASP.NET Core MVC..."
Write-Host "=========================================="

if (Test-Path $PublishDir) {
    Remove-Item -Recurse -Force $PublishDir
}

dotnet publish -c Release -r linux-x64 --self-contained true -o $PublishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Fallo la publicacion del proyecto."
    exit 1
}

foreach ($Nodo in $Nodos) {
    Write-Host ""
    Write-Host "=========================================="
    Write-Host "Desplegando frontend en nodo $Nodo"
    Write-Host "=========================================="

    ssh $Usuario@$Nodo "rm -rf /tmp/frontendfacturacion && mkdir -p /tmp/frontendfacturacion"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: No se pudo preparar la carpeta temporal en $Nodo"
        exit 1
    }

    scp -r "$PublishDir/*" "$Usuario@${Nodo}:/tmp/frontendfacturacion/"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Fallo la copia hacia el nodo $Nodo"
        exit 1
    }

    Write-Host "Aplicando despliegue dentro del nodo $Nodo..."

    ssh -t $Usuario@$Nodo "sudo bash -c 'systemctl stop frontendfacturacion && rm -rf /var/www/frontendfacturacion/* && cp -r /tmp/frontendfacturacion/* /var/www/frontendfacturacion/ && chown -R frontend:frontend /var/www/frontendfacturacion && chmod +x /var/www/frontendfacturacion/FrontendFacturacion && chcon -t bin_t /var/www/frontendfacturacion/FrontendFacturacion && systemctl start frontendfacturacion && systemctl restart nginx && systemctl status frontendfacturacion --no-pager'"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Fallo el despliegue interno en el nodo $Nodo"
        exit 1
    }

    Write-Host "Nodo $Nodo actualizado correctamente."
}

Write-Host ""
Write-Host "=========================================="
Write-Host "Despliegue finalizado en ambos nodos."
Write-Host "=========================================="