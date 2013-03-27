padiFS
======

IST Distributed Applications Course Project


### Implementation status

Items marked as "✘" are not planned for implementation. 


<table>
  <thead>
    <tr><th>Object / Feature</th><th>Status</th><th>Notes</th></tr>
  </thead>
  <tbody>
    <tr><td>CanvasRenderingContext2D</td><td></td><td></td></tr>
    <tr><td>- state - save/restore</td><td>✔</td><td></td></tr>
    <tr><td>- matrix transformations: scale, transform, etc</td><td>✔</td><td></td></tr>
    <tr><td>- compositing - alpha, composite operation</td><td>✔</td><td></td></tr>
    <tr><td>- image smoothing</td><td>✔</td><td></td></tr>
    <tr><td>- stroke/fill style</td><td>✔</td><td></td></tr>
    <tr><td>- solid colors</td><td>✔</td><td></td></tr>
    <tr><td>- gradients</td><td>✔</td><td></td></tr>
    <tr><td>- patterns</td><td>✔</td><td><br>(see below)</td></tr>
    <tr><td>- shadows</td><td>✔</td><td></td></tr>
    <tr><td>- clear/fill/stroke rect</td><td>✔</td><td></td></tr>
    <tr><td>- beginPath, paths / path methods, fill, stroke</td><td>✔</td><td></td></tr>
    <tr><td>- focus ring</td><td>✘</td><td></td></tr>
    <tr><td>- scrollPathIntoView</td><td>✘</td><td></td></tr>
    <tr><td>- clipping region</td><td>✔</td><td></td></tr>
    <tr><td>- isPointInPath</td><td>to do</td><td>Support planned.</td></tr>
    <tr><td>- fill/stroke text</td><td>✔</td><td></td></tr>
    <tr><td>- measure text</td><td>✔</td><td>hanging and ideographic baselines not implemented.</td></tr>
    <tr><td>- drawImage</td><td>✔</td><td></td></tr>
    <tr><td>- hit regions</td><td>✘</td><td></td></tr>
    <tr><td>- create/get/put image data</td><td>✔</td><td></td></tr>
    <tr><td>CanvasDrawingStyles</td><td></td><td></td></tr>
    <tr><td>- line caps/joins - line width, cap, join, miter limit</td><td>✔</td><td></td></tr>
    <tr><td>- dashed lines</td><td>✔</td><td></td></tr>
    <tr><td>- text - font, textAlign, textBaseline</td><td>✔</td><td></td></tr>
    <tr><td>CanvasPathMethods</td><td>✔</td><td></td></tr>
    <tr><td>- beginPath</td><td>✔</td><td>Also available on the Path object.</td></tr>
    <tr><td>- moveTo, lineTo</td><td>✔</td><td></td></tr>
    <tr><td>- quadraticCurveTo, bezierCurveTo</td><td>✔</td><td>Untested</td></tr>
    <tr><td>- arcTo</td><td>✔</td><td>Implemented Canvas Level 2 (elliptical arcs)</td></tr>
    <tr><td>- rect</td><td>✔</td><td></td></tr>
    <tr><td>- arc</td><td>✔</td><td></td></tr>
    <tr><td>- ellipse</td><td>✔</td><td></td></tr>
    <tr><td>CanvasGradient</td><td>✔</td><td></td></tr>
    <tr><td>- addColorStop</td><td>✔</td><td></td></tr>
    <tr><td>CanvasPattern</td><td>✔</td><td>OpenVG doesn't support one-directional patterns. For now only 'no-repeat' and 'repeat' work as expected.</td></tr>
    <tr><td>- setTransform</td><td>to do</td><td>Planned.</td></tr>
    <tr><td>TextMetrics</td><td>✔</td><td></td></tr>
    <tr><td>HitRegionOptions</td><td>✘</td><td></td></tr>
    <tr><td>ImageData</td><td>✔</td><td></td></tr>
    <tr><td>Path</td><td>✔</td><td>(see CanvasPathMethods)</td></tr>
    <tr><td>- (constructor)</td><td>✔</td><td>SVG path constructor after v1.0</td></tr>
    <tr><td>- addPath</td><td>✔</td><td></td></tr>
    <tr><td>- addPathByStrokingPath</td><td>✘</td><td></td></tr>
    <tr><td>- addText</td><td>✔</td><td>Position and Path variants</td></tr>
    <tr><td>- addPathByStrokingText</td><td>✘</td><td></td></tr>
  </tbody>
</table>

## License

(The MIT License)

Copyright (c) 2012 Luis Reis

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


