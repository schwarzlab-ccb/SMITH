out=$1

[[ -z "$out" ]] && { echo "No path provided for multiplot sampling. A path with SMITH output subfolders should be included." ; exit 1; }

echo "Traversing the folder $out" 

cd "$out"
for d in */ ; do
    cd $d
    echo "Plotting the sub-folder $d"
    if [[ -f "populations.csv" && -f "parent_tree.csv" ]]; then
        echo "Plotting Fish Plot"
        col=42
        smooth=2
        pyfish populations.csv parent_tree.csv fish.png -R $col -S $smooth
        pyfish populations.csv parent_tree.csv fish_abs.png -R $col -S $smooth -a  
    else
        echo "File populations.csv or parent_tree.csv does not exist. Skipping fish plot."
    fi
    echo "Plotting Clone Tree"
    dot -Tpng clone_tree.dot > clone_tree.png
    cd ..
done
cd ..