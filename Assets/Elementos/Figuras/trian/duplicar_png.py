#!/usr/bin/env python3
import os
import shutil
import argparse

def duplicar_png(original_path, colores):
    # Verificar existencia del archivo y extensión
    if not os.path.isfile(original_path):
        raise FileNotFoundError(f"No existe el archivo: {original_path}")
    nombre, ext = os.path.splitext(os.path.basename(original_path))
    if ext.lower() != ".png":
        raise ValueError(f"El archivo debe ser .png, no '{ext}'")

    directorio = os.path.dirname(os.path.abspath(original_path)) or os.getcwd()

    for color in colores:
        nuevo_nombre = f"{nombre} {color}.png"
        destino = os.path.join(directorio, nuevo_nombre)
        shutil.copyfile(original_path, destino)
        print(f"Creado: {destino}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Duplica un archivo .png varias veces añadiendo un sufijo de color al nombre."
    )
    parser.add_argument(
        "archivo",
        help="Ruta al archivo PNG original que quieres copiar."
    )
    # Aquí puedes ajustar o alargar la lista de colores si gustas
    parser.add_argument(
        "--colores",
        nargs="+",
        default=["Blanco", "Rojo", "Azul", "Amarillo", "Rosa", "Verde", "Morado", "Cafe", "Gris"],
        help="Lista de sufijos (colores) a usar en los nuevos nombres."
    )
    args = parser.parse_args()

    duplicar_png(args.archivo, args.colores)
