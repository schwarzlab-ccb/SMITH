import argparse
import re


def convert_dot_to_newick(input_file, output_file=None):
    def recursive_tree_generation(node):
        if node in leaves:
            return f"{node}-{nodes[node]}"
        elif node == root:
            children = [(e, l) for s, e, l in zip(edges_start, edges_end, edges_length) if node == s]
            child_strings = []
            for child, length in children:
                child_strings.append(recursive_tree_generation(child) + f":{length}")
            return "(" + ", ".join(child_strings) + f")"
        else:
            children = [(e, l) for s, e, l in zip(edges_start, edges_end, edges_length) if node == s]
            child_strings = []
            for child, length in children:
                child_strings.append(recursive_tree_generation(child) + f":{length}")
            return "(" + ", ".join(child_strings) + f"){node}-{nodes[node]}"


    with open(input_file) as f:
        dotfile = f.readlines()

    nodes = [x.strip() for x in dotfile if "label" in x and "->" not in x]
    nodes = [re.search('(.*) \[label=.*:(.*)"', n).groups() for n in nodes]
    nodes = {k: v for (k, v) in nodes}

    edges = [x.strip() for x in dotfile if "label" in x and "->" in x]

    edges_start = [e.split('->')[0].strip() for e in edges]
    edges_end = [e.split('->')[1].split('[')[0].strip() for e in edges]
    edges_length = [e.split('label="')[1].replace('"];', '') for e in edges]
    leaves = [n for n in nodes.keys() if n not in edges_start]
    root = [n for n in nodes.keys() if n not in edges_end]
    assert len(root) == 1, f"multiple roots?! Roots: {root}"
    root = root[0]

    output = recursive_tree_generation(root)
    output = output[:-1] + f", {root}-{nodes[root]}:0):0;\n"

    outname = output_file if output_file is not None else input_file.replace(".dot", ".new")

    with open(outname, 'w') as f:
        dotfile = f.write(output)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("input_file", help="a path to the input file")
    parser.add_argument("--output_file", type=str, default=None, required=False)
    args = parser.parse_args()

    convert_dot_to_newick(args.input_file, args.output_file)
