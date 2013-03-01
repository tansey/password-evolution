import csv
import os
import numpy as np
import matplotlib.pyplot as plt
import sys
from itertools import product

models = ['First-Order', '8-Layer']
sites = ['faithwriters', 'myspace', 'phpbb', 'rockyou', 'singles.org']

def last_row(filename):
    f = open(filename, 'r')
    reader = csv.reader(f)
    row = reader.next()
    for r in reader:
        if len(r) == 5:
            row = r
    # Guesses, Accounts Cracked, Passwords Cracked, % Accounts, % Passwords
    return row

for target in sites:
    print 'Generating plot for {0}'.format(target)
    colors = ['red', 'blue']
    ax = plt.subplot(111)
    series = [[],[]]
    for midx,model in enumerate(models):
        for training in sites:
            passwords_cracked = last_row('{0}-{1}-{2}.csv'.format(model.lower(), training, target))[-1]
            series[midx].append(float(passwords_cracked.replace("%","")))
    ind = np.arange(len(sites)) * 1.5 + 0.5
    width = 0.35
    bars = [ax.bar(ind+width*sidx, s, width, color=colors[sidx])[0] for (sidx, s) in enumerate(series)]
    ax.set_ylabel('% of Passwords Cracked')
    ax.set_xlabel('Training Dataset')
    ax.set_title('Performance on {0}'.format(target))
    ax.set_xticks(ind+width)
    ax.set_xticklabels(sites)
    box = ax.get_position()
    ax.set_position([box.x0, box.y0, box.width * 0.8, box.height])
    ax.legend(bars, models, loc='center left', bbox_to_anchor=(1,0.5))
    plt.savefig('{0}.png'.format(target))
    plt.clf()