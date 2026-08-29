FROM debian:bullseye-slim

# Install wine and dependencies
RUN dpkg --add-architecture i386 && \
    apt-get update && \
    apt-get install -y \
    wine64 wine32 \
    wget unzip \
    xvfb \
    screen \
    && rm -rf /var/lib/apt/lists/*

# Create user
RUN useradd -m warband
USER warband
WORKDIR /home/warband/server

# Copy server files (you need to download dedicated server separately)
# For build, place mb_warband_dedicated_*.zip in context
# Or download via script

COPY --chown=warband:warband nordinvasion.cfg ./
COPY --chown=warband:warband start_server.sh ./
RUN chmod +x start_server.sh

# Volume for module
VOLUME /home/warband/server/Modules/Fianna_NordInvasion

EXPOSE 7240/udp
EXPOSE 7241/udp

CMD ["bash", "-c", "xvfb-run -a wineconsole --backend=curses mb_warband_dedicated.exe -r nordinvasion.cfg -m Fianna_NordInvasion"]
